using Avalonia.Controls;
using Avalonia.Interactivity;
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
        EarthquakeInfo? buffer = null;
        btnExample.IsEnabled = false;
        try
        {
            buffer = JsonSerializer.Deserialize<EarthquakeInfo>(JsonSerializer.Serialize(Settings.EarthquakeInfo));
            // 演练期间屏蔽真实预警动作，避免误执行关机等命令。
            Settings.IsDrillMode = true;

            DateTime startTime = DateTime.Now;
            // 每次演练使用独立事件 ID，确保能重新触发语音和音量增强。
            var drillEventId = $"example-event-{Guid.NewGuid():N}";
            for (int i = 0; i < 6; i++)
            {
                Settings.EarthquakeInfo.UpdateFrom(new EarthquakeInfo
                {
                    Id = $"example-{i}",
                    EventId = drillEventId,
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"预警演练失败: {ex}");
        }
        finally
        {
            if (buffer is not null)
            {
                Settings.EarthquakeInfo.UpdateFrom(buffer);
            }
            Settings.IsDrillMode = false;
            btnExample.IsEnabled = true;
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
}
