using System.Windows.Controls;
using EarthquakeWarning.Services.NotificationProviders;
using Octokit;
using PluginWithNotificationProviders.Models;

namespace PluginWithNotificationProviders.Controls.NotificationProviders;

public partial class EarthquakeNotificationProviderSettingsControl : UserControl
{
    public EarthquakeNotificationSettings Settings { get; }
    private EarthquakeNotificationProvider EarthquakeNotificationProvider { get; }

    public EarthquakeNotificationProviderSettingsControl(EarthquakeNotificationSettings settings, EarthquakeNotificationProvider earthquakeNotificationProvider)
    {
        Settings = settings;
        EarthquakeNotificationProvider = earthquakeNotificationProvider;
        InitializeComponent();
    }

    private async void btnExample_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        btnExample.IsEnabled = false;
        await Task.Run(EarthquakeNotificationProvider.Example);
        btnExample.IsEnabled = true;
    }
}