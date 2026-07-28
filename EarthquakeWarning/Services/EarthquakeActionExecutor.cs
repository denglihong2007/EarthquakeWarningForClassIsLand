using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

namespace EarthquakeWarning.Services;

internal static class EarthquakeActionExecutor
{
    private static readonly SemaphoreSlim AudioSemaphore = new(1, 1);

    public static void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException("CMD 命令为空。");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("无法启动 CMD 命令。");
        }
    }

    public static async Task PlayAudioAtMaximumVolumeAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("找不到配置的预警音频文件。", filePath);
        }

        await AudioSemaphore.WaitAsync();
        try
        {
            using var deviceEnumerator = new MMDeviceEnumerator();
            using var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var endpointVolume = device.AudioEndpointVolume;
            var originalVolume = endpointVolume.MasterVolumeLevelScalar;
            var wasMuted = endpointVolume.Mute;

            try
            {
                endpointVolume.Mute = false;
                endpointVolume.MasterVolumeLevelScalar = 1.0f;
                await PlayAudioAsync(filePath);
            }
            finally
            {
                endpointVolume.MasterVolumeLevelScalar = originalVolume;
                endpointVolume.Mute = wasMuted;
            }
        }
        finally
        {
            AudioSemaphore.Release();
        }
    }

    private static async Task PlayAudioAsync(string filePath)
    {
        using var audioReader = new MediaFoundationReader(filePath);
        using var outputDevice = new WaveOutEvent { Volume = 1.0f };
        var playbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        outputDevice.PlaybackStopped += (_, args) =>
        {
            if (args.Exception is not null)
            {
                playbackCompleted.TrySetException(args.Exception);
            }
            else
            {
                playbackCompleted.TrySetResult();
            }
        };

        outputDevice.Init(audioReader);
        outputDevice.Play();
        await playbackCompleted.Task;
    }
}
