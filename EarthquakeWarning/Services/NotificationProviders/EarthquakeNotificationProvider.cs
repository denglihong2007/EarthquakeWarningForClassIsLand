using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using EarthquakeWarning.Calculators;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Converters;
using EarthquakeWarning.Models;
using System.Diagnostics;
using System.Text.Json;

namespace EarthquakeWarning.Services.NotificationProviders;

[NotificationProviderInfo("B27C0AF3-C917-44DE-A61D-8010C3F3FB92", "地震预警", "\uEF5C", "在地震发生时，根据用户设置发出地震预警")]
public class EarthquakeNotificationProvider : NotificationProviderBase<EarthquakeNotificationSettings>
{
    private const int PollIntervalMs = 1000;
    private const int ReconnectDelayMs = 5000;
    private static readonly Uri EewUri = new("https://api.wolfx.jp/cenc_eew.json");
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    public EarthquakeNotificationProvider()
    {
        Task.Run(EewMonitor);
        Task.Run(DataMonitor);
    }

    private bool _showing = false;
    private string? _lastEewKey;

    private async Task DataMonitor()
    {
        while (true)
        {
            try
            {
                await Task.Delay(500);
                var obj = Settings.EarthquakeInfo;
                if (string.IsNullOrWhiteSpace(obj.Id)) continue;
                double distance = HuaniaEarthQuakeCalculator.GetDistance(Settings.Latitude, Settings.Longitude, obj.Latitude, obj.Longitude);
                double localIntensity = HuaniaEarthQuakeCalculator.GetIntensity(obj.Magnitude, distance);
                Settings.Info = $"在{obj.ShockTime}时，{obj.PlaceName}({obj.Latitude} {obj.Longitude})发生{obj.Magnitude}级地震，震源深度{(obj.Depth is null ? "未知" : obj.Depth)}km。本地距离{distance:F0}km，本地烈度{localIntensity:F1}。";
                if (localIntensity > Settings.Threshold && !_showing)
                {
                    double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(obj.Depth??10.0, distance);
                    DateTime pWaveArriveTime = EarthquakeTime.Parse(obj.ShockTime).AddSeconds(expectTime);
                    if (DateTime.Now >= pWaveArriveTime)
                    {
                        continue;
                    }
                    _showing = true;
                    await Dispatcher.UIThread.InvokeAsync(async () => await ShowNotificationAsync((pWaveArriveTime - DateTime.Now).TotalSeconds, localIntensity));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DataMonitor 错误: " + ex.Message);
            }
        }
    }

    private async Task ShowNotificationAsync(double expectTime, double localIntensity)
    {
        var mask = NotificationContent.CreateTwoIconsMask("地震预警", "\uEF5D", "\uED35");
        mask.Duration = TimeSpan.FromSeconds(3);
        mask.Color = new SolidColorBrush(IntensityToColorConverter.GetColor(localIntensity));
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

    public async Task EewMonitor()
    {
        while (true)
        {
            try
            {
                var json = await HttpClient.GetStringAsync(EewUri);
                var eew = JsonSerializer.Deserialize<WolfxCencEew>(json);
                if (eew is not null &&
                    !string.IsNullOrWhiteSpace(eew.Id) &&
                    !string.IsNullOrWhiteSpace(eew.OriginTime))
                {
                    var earthquakeInfo = eew.ToEarthquakeInfo();
                    var eewKey = $"{eew.Id}:{eew.ReportNum}";
                    if (eewKey != _lastEewKey)
                    {
                        Settings.EarthquakeInfo.UpdateFrom(earthquakeInfo);
                        _lastEewKey = eewKey;
                    }

                    Settings.ServerInfo = $"上一次数据更新：{EarthquakeTime.Format(DateTime.Now)}";
                }

                await Task.Delay(PollIntervalMs);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"获取地震预警数据失败: {e.Message}，将在 {ReconnectDelayMs / 1000} 秒后重试。");
                await Task.Delay(ReconnectDelayMs);
            }
        }
    }
}
