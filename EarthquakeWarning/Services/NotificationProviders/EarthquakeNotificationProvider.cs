using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using EarthquakeWarning.Calculators;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Converters;
using EarthquakeWarning.Models;
using EarthquakeWarning.Services;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Net.Http;

namespace EarthquakeWarning.Services.NotificationProviders;

[NotificationProviderInfo("B27C0AF3-C917-44DE-A61D-8010C3F3FB92", "地震预警", "\uEF5C", "在地震发生时，根据用户设置发出地震预警")]
public class EarthquakeNotificationProvider : NotificationProviderBase<EarthquakeNotificationSettings>
{
    private const int PollIntervalMs = 1000;
    private const int ReconnectDelayMs = 5000;
    private const int SpeechCountdownStartSeconds = 10;
    private const int EarlySpeechStepSeconds = 2;
    private static readonly TimeSpan SpeechPollInterval = TimeSpan.FromMilliseconds(100);
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

    private volatile bool _showing = false;
    private string? _lastMiuiKey;
    private string? _lastActionEventId;
    private string? _lastSpeechEventId;
    private string? _lastShownEventId;
    private CancellationTokenSource _countdownSpeechCts = new();

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
                double localIntensity = HuaniaEarthQuakeCalculator.GetIntensity(obj.Magnitude, distance, obj.Depth, obj.Longitude);
                // 深度缺失或为 0 时按 10 km 处理，与烈度估算保持一致。
                double depthKm = obj.Depth is > 0 ? obj.Depth.Value : 10.0;
                var depthText = obj.Depth is > 0 ? $"{depthKm:F0}km" : "未知";
                var info = $"在{obj.ShockTime}时，{obj.PlaceName}({obj.Latitude} {obj.Longitude})发生{obj.Magnitude}级地震，震源深度{depthText}。震中距{distance:F0}km，本地烈度{localIntensity:F1}。";
                // 设置项绑定在 UI 线程，避免后台线程直接触发属性变更。
                Dispatcher.UIThread.Post(() => Settings.Info = info);

                var speechShouldTrigger = Settings.IsSpeechEnabled && localIntensity > Settings.SpeechThreshold;
                var notificationShouldTrigger = localIntensity > Settings.Threshold;
                if (!speechShouldTrigger && !notificationShouldTrigger)
                {
                    continue;
                }

                double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(depthKm, distance);
                DateTime pWaveArriveTime = EarthquakeTime.Parse(obj.ShockTime).AddSeconds(expectTime);
                if (DateTime.Now >= pWaveArriveTime)
                {
                    continue;
                }

                // 语音播报使用独立的开关和阈值，与通知阈值互不影响。
                if (speechShouldTrigger
                    && !string.IsNullOrWhiteSpace(obj.EventId) && obj.EventId != _lastSpeechEventId)
                {
                    _lastSpeechEventId = obj.EventId;
                    StartCountdownSpeech(pWaveArriveTime);
                }

                if (!notificationShouldTrigger)
                {
                    continue;
                }

                // MIUI eventId is stable across all updates of the same earthquake.
                if (!string.IsNullOrWhiteSpace(obj.EventId) && obj.EventId != _lastActionEventId)
                {
                    if (Settings.IsDrillMode)
                    {
                        // 不把演练事件记入去重状态，避免演练结束后对真实事件重复执行动作。
                        SetActionStatus("预警演练中，未执行预警动作");
                    }
                    else
                    {
                        _lastActionEventId = obj.EventId;
                        _ = ExecuteConfiguredActionsAsync();
                    }
                }

                if (_showing)
                {
                    continue;
                }

                // 同一地震只弹一次提醒，后续更新通过共享的 EarthquakeInfo 实时反映。
                var eventKey = string.IsNullOrWhiteSpace(obj.EventId) ? obj.Id : obj.EventId;
                if (eventKey == _lastShownEventId)
                {
                    continue;
                }

                _showing = true;
                _lastShownEventId = eventKey;
                // 不阻塞监测循环：提醒显示期间仍需继续处理地震更新。
                _ = ShowNotificationSafeAsync(pWaveArriveTime, localIntensity);
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
            SetActionStatus("未启用 CMD 命令");
        }
    }

    private void StartCountdownSpeech(DateTime arriveTime)
    {
        _countdownSpeechCts.Cancel();
        _countdownSpeechCts = new CancellationTokenSource();
        _ = SpeakCountdownAsync(arriveTime, _countdownSpeechCts.Token);
    }

    private static async Task SpeakCountdownAsync(DateTime arriveTime, CancellationToken cancellationToken)
    {
        IDisposable? volumeBoost = null;
        var boostAttempted = false;
        var countdownStarted = false;
        var lastSpoken = int.MaxValue;

        try
        {
            ISpeechService? speechService = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var exactRemaining = (arriveTime - DateTime.Now).TotalSeconds;
                var remaining = (int)Math.Ceiling(exactRemaining);
                if (remaining <= 0)
                {
                    break;
                }

                // 预警一开始就提高系统音量，并在整个倒计时期间保持，结束后恢复。
                if (!boostAttempted)
                {
                    boostAttempted = true;
                    volumeBoost = SystemVolumeBoost.Acquire();
                }

                var inFinalCountdown = remaining <= SpeechCountdownStartSeconds;

                // 最后 10 秒逐秒播报；在此之前每 2 秒播报一次。
                var shouldSpeak = inFinalCountdown
                    ? remaining != lastSpoken
                    : remaining % EarlySpeechStepSeconds == 0 && remaining != lastSpoken;
                if (shouldSpeak)
                {
                    if (!countdownStarted)
                    {
                        countdownStarted = true;
                        // 播报期间让 ClassIsland 的语音队列让路，避免与倒计时重叠。
                        speechService = IAppHost.TryGetService<ISpeechService>();
                        speechService?.ClearSpeechQueue();
                    }

                    var text = remaining.ToString(CultureInfo.InvariantCulture);
                    // 优先使用本地语音，避免在线语音服务的延迟；不可用时退回 ClassIsland 语音服务。
                    if (!LocalSpeech.TrySpeak(text))
                    {
                        speechService ??= IAppHost.TryGetService<ISpeechService>();
                        speechService?.EnqueueSpeechQueue(text);
                    }

                    lastSpoken = remaining;
                }

                TimeSpan delay;
                if (inFinalCountdown)
                {
                    delay = SpeechPollInterval;
                }
                else
                {
                    // 最多休眠 1 秒，保证能踩到每个偶数秒；临近倒数时精确等待到 10 秒整。
                    delay = TimeSpan.FromSeconds(Math.Clamp(exactRemaining - SpeechCountdownStartSeconds, 0.05, 1));
                }

                await Task.Delay(delay, cancellationToken);
            }

            if (volumeBoost is not null)
            {
                // 留出最后一个数字的播放时间。
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"倒计时语音播报失败: {ex.Message}");
        }
        finally
        {
            volumeBoost?.Dispose();
        }
    }

    private void SetActionStatus(string status)
    {
        Dispatcher.UIThread.Post(() => Settings.ActionStatus = status);
    }

    private async Task ShowNotificationSafeAsync(DateTime arriveTime, double localIntensity)
    {
        try
        {
            var expectTime = (arriveTime - DateTime.Now).TotalSeconds;
            await Dispatcher.UIThread.InvokeAsync(async () => await ShowNotificationAsync(expectTime, localIntensity));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示地震预警失败: {ex}");
        }
        finally
        {
            _showing = false;
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
                // 至少保留 1 秒，避免临近到达时出现负时长。
                Duration = TimeSpan.FromSeconds(Math.Max(1.0, expectTime - 3.0)),
            }

        };
        await ShowNotificationAsync(notice);
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
                        _lastMiuiKey = miuiKey;
                        var info = record.ToEarthquakeInfo();
                        Dispatcher.UIThread.Post(() => Settings.EarthquakeInfo.UpdateFrom(info));
                    }

                    var serverInfo = $"上一次数据更新：{EarthquakeTime.Format(DateTime.Now)}";
                    Dispatcher.UIThread.Post(() => Settings.ServerInfo = serverInfo);
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
