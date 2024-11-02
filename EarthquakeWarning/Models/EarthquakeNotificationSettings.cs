using CommunityToolkit.Mvvm.ComponentModel;

namespace PluginWithNotificationProviders.Models;

public class EarthquakeNotificationSettings : ObservableRecipient
{
    private double _latitude = 28.741;
    private double _longitude = 104.850;
    private double _threshold = 2.0;
    public double Latitude
    {
        get => _latitude;
        set
        {
            if (value == _latitude) return;
            _latitude = value;
            OnPropertyChanged();
        }
    }
    public double Longitude
    {
        get => _longitude;
        set
        {
            if (value == _longitude) return;
            _longitude = value;
            OnPropertyChanged();
        }
    }
    public double Threshold
    {
        get => _threshold;
        set
        {
            if (value == _threshold) return;
            _threshold = value;
            OnPropertyChanged();
        }
    }
}