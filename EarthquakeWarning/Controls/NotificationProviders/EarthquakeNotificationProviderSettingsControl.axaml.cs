using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Platforms.Abstraction;
using EarthquakeWarning.Models;
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
        DateTime StartTime = DateTime.Now;
        int eventId = 0;
        for (int i = 0; i < 6; i++)
        {
            Settings.EarthquakeInfo.UpdateFrom(new EarthquakeInfo
            {
                Type = "Earthquake",
                Data = new Data
                {
                    EventId = $"{i}",
                    Updates = i + 1,
                    Latitude = 31.0,
                    Longitude = 103.4,
                    Depth = 14,
                    PlaceName = "四川省阿坝藏族羌族自治州汶川县",
                    ShockTime = StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Magnitude = 4.0 + (i + 1.0) * 0.8,
                    EpiIntensity = 12
                },
                Md5 = "example-md5-hash-" + (eventId + i)
            });
            await Task.Delay(10000);
        }
        await Task.Delay(20000);
        btnExample.IsEnabled = true;
        Settings.EarthquakeInfo.UpdateFrom(buffer);
    }

    private async void btnLocate_Click(object? sender, RoutedEventArgs e)
    {
        btnLocate.IsEnabled = false;
        var location = await PlatformServices.LocationService.GetLocationAsync();
        if(location is not null)
        {
            Settings.Longitude = location.Longitude;
            Settings.Latitude = location.Latitude;
        }
        btnLocate.IsEnabled = true;
    }
}
