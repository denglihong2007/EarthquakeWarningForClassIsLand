using System.Diagnostics;
using System.Speech.Synthesis;

namespace EarthquakeWarning.Services;

/// <summary>
/// 使用 Windows 本地语音合成（SAPI）播报数字。
/// 不依赖 ClassIsland 中可能配置的在线语音服务，避免网络延迟导致倒计时不同步。
/// </summary>
internal static class LocalSpeech
{
    private static readonly object SyncRoot = new();
    private static SpeechSynthesizer? _synthesizer;
    private static bool _unavailable;

    /// <summary>
    /// 立即播报一段文本。返回 false 表示本地语音不可用，调用方应自行降级。
    /// </summary>
    public static bool TrySpeak(string text)
    {
        lock (SyncRoot)
        {
            if (_unavailable)
            {
                return false;
            }

            try
            {
                if (_synthesizer is null)
                {
                    var synthesizer = new SpeechSynthesizer();
                    var chineseVoice = synthesizer.GetInstalledVoices()
                        .FirstOrDefault(v => v.Enabled
                            && v.VoiceInfo.Culture.TwoLetterISOLanguageName == "zh");
                    if (chineseVoice is not null)
                    {
                        synthesizer.SelectVoice(chineseVoice.VoiceInfo.Name);
                    }

                    // 稍微加快语速，保证一个数字能在一秒内念完。
                    synthesizer.Rate = 1;
                    _synthesizer = synthesizer;
                }

                // 丢弃尚未播完的上一个数字，避免语音排队导致越播越晚。
                _synthesizer.SpeakAsyncCancelAll();
                _synthesizer.SpeakAsync(text);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"本地语音播报失败: {ex}");
                _unavailable = true;
                _synthesizer?.Dispose();
                _synthesizer = null;
                return false;
            }
        }
    }
}
