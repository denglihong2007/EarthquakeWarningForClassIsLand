using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Platforms.Abstraction;
using EarthquakeWarning.Calculators;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Converters;
using EarthquakeWarning.Models;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EarthquakeWarning.Services.NotificationProviders;

[NotificationProviderInfo("B27C0AF3-C917-44DE-A61D-8010C3F3FB92", "地震预警", "\uEF5C", "在地震发生时，根据用户设置发出地震预警")]
public class EarthquakeNotificationProvider : NotificationProviderBase<EarthquakeNotificationSettings>
{
    private const int HeartbeatIntervalMs = 60000;
    private const int ReconnectDelayMs = 5000;
    private static readonly Uri NewUri = new("wss://ws.fanstudio.tech/cea-pr");

    private readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public EarthquakeNotificationProvider()
    {
        Task.Run(WSMonitor);
        Task.Run(DataMonitor);
    }

    private bool _showing = false;

    private async Task DataMonitor()
    {
        while (true)
        {
            try
            {
                await Task.Delay(500);
                var obj = Settings.EarthquakeInfo.Data;
                if (obj is null) continue;
                double distance = HuaniaEarthQuakeCalculator.GetDistance(Settings.Latitude, Settings.Longitude, obj.Latitude, obj.Longitude);
                double threshold = HuaniaEarthQuakeCalculator.GetIntensity(obj.Magnitude, distance);
                Settings.Info = $"在{obj.ShockTime}时，{obj.PlaceName}({obj.Latitude} {obj.Longitude})发生{obj.Magnitude}级地震，震源深度{obj.Depth}km。本地距离{distance:F0}km，本地烈度{threshold:F1}。";
                if (threshold > Settings.Threshold && !_showing)
                {
                    double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(obj.Depth, distance);
                    DateTime pWaveArriveTime = DateTime.Parse(obj.ShockTime).AddSeconds(expectTime);
                    if (DateTime.Now >= pWaveArriveTime)
                    {
                        continue;
                    }
                    _showing = true;
                    await Dispatcher.UIThread.InvokeAsync(async () => await ShowNotificationAsync((pWaveArriveTime - DateTime.Now).TotalSeconds));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DataMonitor 错误: " + ex.Message);
            }
        }
    }

    private async Task ShowNotificationAsync(double expectTime)
    {
        var mask = NotificationContent.CreateTwoIconsMask("地震预警", "\uEF5D", "\uED35");
        mask.Duration = TimeSpan.FromSeconds(3);
        mask.Color = new SolidColorBrush((new IntensityToColorConverter().Convert(Settings.Threshold, null, null, null) as Avalonia.Media.Color? ?? Avalonia.Media.Colors.Red));
        var notice = new NotificationRequest
        {
            MaskContent = mask,
            OverlayContent = new NotificationContent()
            {
                Content = new EarthquakeNotificationProviderControl(Settings.EarthquakeInfo, Settings.Latitude, Settings.Longitude),
                Duration = TimeSpan.FromSeconds(expectTime - 3),
            }

        };
        await ShowNotificationAsync(notice);
        _showing = false;
    }

    public async Task WSMonitor()
    {
        while (true)
        {
            using var ws = new ClientWebSocket();
            using var cts = new CancellationTokenSource();
            var buffer = new byte[4096];
            try
            {
                await ws.ConnectAsync(NewUri, cts.Token);
                var heartbeatTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(HeartbeatIntervalMs, cts.Token);
                    }
                }, cts.Token);

                while (ws.State == WebSocketState.Open)
                {
                    using var receiveCts = new CancellationTokenSource(HeartbeatIntervalMs * 2); 
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, receiveCts.Token);

                    var receiveTask = ws.ReceiveAsync(buffer, linkedCts.Token);
                    var completedTask = await Task.WhenAny(receiveTask, heartbeatTask);

                    if (completedTask == heartbeatTask)
                    {
                        Debug.WriteLine("⚠️ 心跳超时，尝试关闭连接并重连...");
                        throw new TimeoutException("Heartbeat timeout, forcing reconnect.");
                    }
                    var result = await receiveTask;
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.WriteLine("⚠️ WebSocket 关闭: " + result.CloseStatus);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var jsonDocument = JsonDocument.Parse(json);
                        if (jsonDocument.RootElement.TryGetProperty("type", out var typeElement) &&
                            typeElement.GetString() == "heartbeat")
                        {
                            if (jsonDocument.RootElement.TryGetProperty("timestamp", out JsonElement timestampElement) && timestampElement.ValueKind != JsonValueKind.Null)
                            {
                                if (timestampElement.TryGetInt64(out long timestampMs))
                                {
                                    DateTime lastHeartbeatTime = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).LocalDateTime;
                                    Settings.ServerInfo = $"上一个心跳包时间戳：{lastHeartbeatTime:yyyy-MM-dd HH:mm:ss}";
                                }
                            }
                            continue;
                        }
                        var earthquakeInfo = JsonSerializer.Deserialize<EarthquakeInfo>(json, options);
                        if (earthquakeInfo?.Data != null && earthquakeInfo.Md5 != Settings.EarthquakeInfo.Md5)
                        {
                            Settings.EarthquakeInfo.UpdateFrom(earthquakeInfo);
                        }
                    }
                }
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Debug.WriteLine($"连接被服务器提前关闭，尝试重连... 错误: {e.Message}");
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("心跳/接收超时，强制重连...");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"连接/接收错误: {e.Message}，将在 {ReconnectDelayMs / 1000} 秒后重试。");
            }
            finally
            {
                cts.Cancel();
                if (ws.State != WebSocketState.Closed)
                {
                    try { ws.Abort(); } catch {  }
                }
            }
            await Task.Delay(ReconnectDelayMs);
        }
    }
}