using System.Windows.Controls;
using PluginWithNotificationProviders.Models;

namespace PluginWithNotificationProviders.Controls.NotificationProviders;

public partial class EarthquakeNotificationProviderSettingsControl : UserControl
{
    public EarthquakeNotificationSettings Settings { get; }

    public EarthquakeNotificationProviderSettingsControl(EarthquakeNotificationSettings settings)
    {
        Settings = settings;

        InitializeComponent();
    }
}