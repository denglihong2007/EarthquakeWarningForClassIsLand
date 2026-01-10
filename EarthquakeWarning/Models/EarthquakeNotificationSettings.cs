using CommunityToolkit.Mvvm.ComponentModel;

namespace EarthquakeWarning.Models;

public partial class EarthquakeNotificationSettings : ObservableRecipient
{
    [ObservableProperty]
    private double _latitude = 0;
    [ObservableProperty]
    private double _longitude = 0;
    [ObservableProperty]
    private double _threshold = 2.0;
    [ObservableProperty]
    private string _info = "";
    [ObservableProperty]
    private EarthquakeInfo earthquakeInfo = new();
    [ObservableProperty]
    private string _serverInfo = "";
}