using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Platforms.Abstraction;
using EarthquakeWarning.Models;
using System.Diagnostics;
using System.Text.Json;

namespace EarthquakeWarning.Controls.NotificationProviders;

public partial class EarthquakeNotificationProviderSettingsControl : NotificationProviderControlBase<EarthquakeNotificationSettings>
{
    public EarthquakeNotificationProviderSettingsControl()
    {
        InitializeComponent();
    }

    private async void btnExample_Click(object sender, RoutedEventArgs e)
    {
        var buffer = JsonSerializer.Deserialize<EarthquakeInfo>(JsonSerializer.Serialize(Settings.EarthquakeInfo));
        btnExample.IsEnabled = false;
        DateTime startTime = DateTime.Now;
        int eventId = 0;
        for (int i = 0; i < 6; i++)
        {
            Settings.EarthquakeInfo.UpdateFrom(new EarthquakeInfo
            {
                Id = $"example-{eventId + i}",
                EventId = "example-event",
                Updates = i + 1,
                Latitude = 31.0,
                Longitude = 103.4,
                Depth = 14,
                PlaceName = "四川省阿坝藏族羌族自治州汶川县",
                ShockTime = EarthquakeTime.Format(startTime),
                UpdateTime = EarthquakeTime.Format(DateTime.Now),
                Magnitude = 4.0 + (i + 1.0) * 0.8,
                EpiIntensity = 12
            });
            await Task.Delay(10000);
        }
        await Task.Delay(20000);
        btnExample.IsEnabled = true;
        if (buffer is not null)
        {
            Settings.EarthquakeInfo.UpdateFrom(buffer);
        }
    }

    private async void btnLocate_Click(object? sender, RoutedEventArgs e)
    {
        btnLocate.IsEnabled = false;
        locationErrorInfoBar.IsOpen = false;

        try
        {
            var location = await PlatformServices.LocationService.GetLocationAsync();
            if (location is null)
            {
                locationErrorInfoBar.IsOpen = true;
                return;
            }

            Settings.Longitude = location.Longitude;
            Settings.Latitude = location.Latitude;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"自动定位失败: {ex}");
            locationErrorInfoBar.IsOpen = true;
        }
        finally
        {
            btnLocate.IsEnabled = true;
        }
    }

    private async void btnSelectAudio_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择地震预警音频",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("音频文件")
                {
                    Patterns = ["*.wav", "*.mp3", "*.aac", "*.m4a", "*.wma"],
                },
            ],
        });

        if (files.Count > 0)
        {
            Settings.AudioFilePath = files[0].Path.LocalPath;
        }
    }
}
