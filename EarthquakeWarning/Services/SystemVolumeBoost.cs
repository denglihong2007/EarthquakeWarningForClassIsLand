using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace EarthquakeWarning.Services;

/// <summary>
/// 临时把系统主音量调到最大，租约结束后恢复原状。
/// 允许多个租约重叠，最后一个租约释放时才恢复。
/// </summary>
internal static class SystemVolumeBoost
{
    private static readonly object SyncRoot = new();
    private static int _leaseCount;
    private static MMDeviceEnumerator? _enumerator;
    private static MMDevice? _device;
    private static AudioEndpointVolume? _volume;
    private static float _originalVolume;
    private static bool _wasMuted;
    private static bool _originalCaptured;

    static SystemVolumeBoost()
    {
        // 进程退出时如果还在倒计时，finally 不一定来得及执行，这里兜底恢复音量。
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            lock (SyncRoot)
            {
                if (_leaseCount == 0)
                {
                    return;
                }

                _leaseCount = 0;
                RestoreVolume();
            }
        };
    }

    public static IDisposable? Acquire()
    {
        lock (SyncRoot)
        {
            if (_leaseCount == 0 && !TryBoost())
            {
                return null;
            }

            _leaseCount++;
            return new Lease();
        }
    }

    private static bool TryBoost()
    {
        try
        {
            _enumerator = new MMDeviceEnumerator();
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _volume = _device.AudioEndpointVolume;
            _originalVolume = _volume.MasterVolumeLevelScalar;
            _wasMuted = _volume.Mute;
            _originalCaptured = true;

            _volume.Mute = false;
            _volume.MasterVolumeLevelScalar = 1.0f;

            // 少数设备会忽略标量音量，退回分贝上限再试一次。
            if (_volume.MasterVolumeLevelScalar < 0.999f)
            {
                _volume.MasterVolumeLevel = _volume.VolumeRange.MaxDecibels;
            }

            Debug.WriteLine(
                $"[地震预警] 音量增强：{_device.FriendlyName} {_originalVolume:F2} -> {_volume.MasterVolumeLevelScalar:F2}");

            // 设备没有真正提高音量时不要持有租约，否则会留下被改动过的音量。
            if (_volume.MasterVolumeLevelScalar <= _originalVolume + 0.001f && !_wasMuted)
            {
                RestoreVolume();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[地震预警] 音量增强失败: {ex}");
            // 可能已经修改了音量后才失败，必须恢复原值。
            RestoreVolume();
            return false;
        }
    }

    private static void Release()
    {
        lock (SyncRoot)
        {
            if (_leaseCount == 0 || --_leaseCount > 0)
            {
                return;
            }

            RestoreVolume();
        }
    }

    private static void RestoreVolume()
    {
        try
        {
            if (_originalCaptured && _volume is not null)
            {
                _volume.MasterVolumeLevelScalar = _originalVolume;
                _volume.Mute = _wasMuted;
            }
        }
        catch
        {
            // 音频设备可能已被移除，忽略恢复失败。
        }
        finally
        {
            _originalCaptured = false;
            ReleaseResources();
        }
    }

    private static void ReleaseResources()
    {
        _volume?.Dispose();
        _device?.Dispose();
        _enumerator?.Dispose();
        _volume = null;
        _device = null;
        _enumerator = null;
    }

    private sealed class Lease : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Release();
            }
        }
    }
}
