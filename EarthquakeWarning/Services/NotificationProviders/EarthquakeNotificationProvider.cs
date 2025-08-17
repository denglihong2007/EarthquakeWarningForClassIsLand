using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using EarthquakeWarning.Calculators;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Models;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace EarthquakeWarning.Services.NotificationProviders;

[NotificationProviderInfo("B27C0AF3-C917-44DE-A61D-8010C3F3FB92", "地震预警", PackIconKind.ShieldHome, "在地震发生时，根据用户设置发出地震预警")]
public class EarthquakeNotificationProvider : NotificationProviderBase<EarthquakeNotificationSettings>
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public LocalPosition LocalPosition { get; }

    public EarthquakeInfo EarthquakeInfo { get; set; } = new();

    private readonly SharedService _sharedService;

    private EarthquakeInfo _bufferedEarthquakeInfo;

    public EarthquakeNotificationProvider(SharedService sharedService)
    {
        _sharedService = sharedService;
        _sharedService.TestInfoUpdated += (sender, info) =>
        {
            _bufferedEarthquakeInfo ??= EarthquakeInfo;
            EarthquakeInfo.UpdateFrom(info);
            Update();
        };
        LocalPosition = new LocalPosition { Latitude = Settings.Latitude, Longitude = Settings.Longitude };
        Task.Run(WSMonitor);
    }

    private bool _showing = false;

    private async void Update()
    {
        var obj = EarthquakeInfo.Data;
        double distance = HuaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude);
        double threshold = HuaniaEarthQuakeCalculator.GetIntensity(double.Parse(obj.Magnitude), distance);
        Settings.Info = $"在{obj.ShockTime}时，{obj.PlaceName}({obj.Latitude} {obj.Longitude})发生{obj.Magnitude}级地震，震源深度{obj.Depth}km。本地距离{distance:F0}km，本地烈度{threshold:F1}。";
        if (threshold > Settings.Threshold && !_showing)
        {
            double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(obj.Depth, distance);
            DateTime pWaveArriveTime = DateTime.Parse(obj.ShockTime).AddSeconds(expectTime);
            if (DateTime.Now >= pWaveArriveTime)
            {
                return;
            }
            _showing = true;
            await Application.Current.Dispatcher.BeginInvoke(async () => await ShowNotificationAsync(expectTime));
        }
    }

    private async Task ShowNotificationAsync(double expectTime)
    {
        var mask = NotificationContent.CreateTwoIconsMask("地震预警", PackIconKind.HomeAlert, PackIconKind.ExitRun);
        mask.Duration = TimeSpan.FromSeconds(3);
        var notice = new NotificationRequest
        {
            MaskContent = mask,
            OverlayContent = new NotificationContent()
            {
                Content = new EarthquakeNotificationProviderControl(EarthquakeInfo, LocalPosition),
                Duration = TimeSpan.FromSeconds(expectTime-3),
            }

        };
        await ShowNotificationAsync(notice);
        _showing = false;
    }

    public async Task WSMonitor()
    {
        using var ws = new ClientWebSocket();
        var uri = new Uri(Encoding.UTF8.GetString(Convert.FromBase64String("d3NzOi8vd3MuZmFuc3R1ZGlvLnRlY2gvaWNs")));

        try
        {
            await ws.ConnectAsync(uri, CancellationToken.None);
            Debug.WriteLine("✅ WebSocket 已连接");

            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.WriteLine("⚠️ WebSocket 关闭: " + result.CloseStatus);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (!_sharedService.Testing && !string.IsNullOrWhiteSpace(json))
                    {
                        var earthquakeInfo = JsonSerializer.Deserialize<EarthquakeInfo>(json, options);
                        if (earthquakeInfo.Data != null && earthquakeInfo.Md5 != EarthquakeInfo.Md5)
                        {
                            EarthquakeInfo.UpdateFrom(earthquakeInfo);
                            Update();
                        }
                        else if (_bufferedEarthquakeInfo != null)
                        {
                            EarthquakeInfo.UpdateFrom(_bufferedEarthquakeInfo);
                            Update();
                            _bufferedEarthquakeInfo = null;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("接收错误: " + ex.Message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine("连接错误: " + e.Message);
        }
    }
}