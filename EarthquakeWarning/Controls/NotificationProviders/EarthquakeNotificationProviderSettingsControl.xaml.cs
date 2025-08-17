using EarthquakeWarning.Services;

namespace EarthquakeWarning.Controls.NotificationProviders;

public partial class EarthquakeNotificationProviderSettingsControl
{
    private readonly SharedService _sharedService;
    public EarthquakeNotificationProviderSettingsControl(SharedService sharedService)
    {
        _sharedService = sharedService;
        InitializeComponent();
    }

    private async void btnExample_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        btnExample.IsEnabled = false;
        await _sharedService.Example();
        btnExample.IsEnabled = true;

    }
}