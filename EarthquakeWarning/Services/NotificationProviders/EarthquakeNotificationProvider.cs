using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using EarthquakeWarning.Calculators;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Converters;
using EarthquakeWarning.Models;
using EarthquakeWarning.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Net.Http;

namespace EarthquakeWarning.Services.NotificationProviders;

[NotificationProviderInfo("B27C0AF3-C917-44DE-A61D-8010C3F3FB92", "地震预警", "\uEF5C", "在地震发生时，根据用户设置发出地震预警")]
public class EarthquakeNotificationProvider : NotificationProviderBase<EarthquakeNotificationSettings>
{
    private const int PollIntervalMs = 1000;
    private const int ReconnectDelayMs = 5000;
    private static readonly Uri MiuiUri = new("https://srv.sec.miui.com/earthquake/warning/records");
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
    private string? _lastMiuiKey;
    private string? _lastActionEventId;

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
                if (localIntensity > Settings.Threshold)
                {
                    double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(obj.Depth??10.0, distance);
                    DateTime pWaveArriveTime = EarthquakeTime.Parse(obj.ShockTime).AddSeconds(expectTime);
                    if (DateTime.Now >= pWaveArriveTime)
                    {
                        continue;
                    }

                    // MIUI eventId is stable across all updates of the same earthquake.
                    if (!string.IsNullOrWhiteSpace(obj.EventId) && obj.EventId != _lastActionEventId)
                    {
                        _lastActionEventId = obj.EventId;
                        _ = ExecuteConfiguredActionsAsync();
                    }

                    if (_showing)
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

    private async Task ExecuteConfiguredActionsAsync()
    {
        var errors = new List<string>();
        var executedActions = new List<string>();
        SetActionStatus("正在执行预警动作...");

        if (Settings.IsCommandEnabled)
        {
            try
            {
                EarthquakeActionExecutor.ExecuteCommand(Settings.Command);
                executedActions.Add("CMD 命令");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行地震预警 CMD 命令失败: {ex}");
                errors.Add($"CMD：{ex.Message}");
            }
        }

        if (Settings.IsAudioEnabled)
        {
            try
            {
                await EarthquakeActionExecutor.PlayAudioAtMaximumVolumeAsync(Settings.AudioFilePath);
                executedActions.Add("音频");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放地震预警音频失败: {ex}");
                errors.Add($"音频：{ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            SetActionStatus(string.Join("；", errors));
        }
        else if (executedActions.Count > 0)
        {
            SetActionStatus($"已执行：{string.Join("、", executedActions)}");
        }
        else
        {
            SetActionStatus("未启用预警动作");
        }
    }

    private void SetActionStatus(string status)
    {
        Dispatcher.UIThread.Post(() => Settings.ActionStatus = status);
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
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["version"] = "2",
                    ["sign"] = MiuiEarthquakeSigning.MakeSignature(new Dictionary<string, string>
                    {
                        ["version"] = "2",
                    }),
                });
                using var response = await HttpClient.PostAsync(MiuiUri, content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MiuiEarthquakeResponse>(json);
                if (result?.Code != 0)
                {
                    throw new InvalidOperationException($"MIUI API returned {result?.Code}: {result?.Description}");
                }

                var record = result.Data?.OrderByDescending(item => item.UpdateAt).FirstOrDefault();
                if (record is not null && record.EventId != 0)
                {
                    var miuiKey = $"{record.EventId}:{record.Update}";
                    if (miuiKey != _lastMiuiKey)
                    {
                        Settings.EarthquakeInfo.UpdateFrom(record.ToEarthquakeInfo());
                        _lastMiuiKey = miuiKey;
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
