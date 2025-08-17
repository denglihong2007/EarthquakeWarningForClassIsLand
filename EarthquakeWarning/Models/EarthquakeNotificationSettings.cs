using CommunityToolkit.Mvvm.ComponentModel;

namespace EarthquakeWarning.Models;

public partial class EarthquakeNotificationSettings : ObservableRecipient
{
    [ObservableProperty]
    private double _latitude = 28.741;
    [ObservableProperty]
    private double _longitude = 104.850;
    [ObservableProperty]
    private double _threshold = 2.0;
    [ObservableProperty]
    private string _info = "";
}