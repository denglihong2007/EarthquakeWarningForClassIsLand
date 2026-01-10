using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using EarthquakeWarning.Calculators;
using EarthquakeWarning.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EarthquakeWarning.Controls.NotificationProviders;
public partial class EarthquakeNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    private double Latitude { get; set; }
    private double Longitude { get; set; }
    public EarthquakeInfo EarthquakeInfo { get; set; }
    public string PlaceName { get; set; }
    public int Updates { get; set; }
    public double Magnitude { get; set; }

    public string LocalIntensity 
    {
        get
        {
            double distance = HuaniaEarthQuakeCalculator.GetDistance(Latitude, Longitude, EarthquakeInfo.Data.Latitude, EarthquakeInfo.Data.Longitude);
            return HuaniaEarthQuakeCalculator.GetIntensity(EarthquakeInfo.Data.Magnitude, distance).ToString("F1");
        }
    }


    public TimeSpan Time 
    {
        get 
        {
            double distance = HuaniaEarthQuakeCalculator.GetDistance(Latitude, Longitude, EarthquakeInfo.Data.Latitude, EarthquakeInfo.Data.Longitude);
            double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(EarthquakeInfo.Data.Depth??10.0, distance);
            DateTime pWaveArriveTime = DateTime.Parse(EarthquakeInfo.Data.ShockTime).AddSeconds(expectTime);
            TimeSpan timeDifference = pWaveArriveTime - DateTime.Now;
            return timeDifference;
        }
        set { }
    }

    public EarthquakeNotificationProviderControl(EarthquakeInfo earthquakeInfo, double latitude,double longitude)
    {
        this.Latitude = latitude;
        this.Longitude = longitude;
        EarthquakeInfo = earthquakeInfo;
        DataContext = this;
        InitializeComponent();
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.5)
        };
        timer.Tick += (s, e) =>
        {
            PlaceName = earthquakeInfo.Data.PlaceName;
            Updates = earthquakeInfo.Data.Updates;
            Magnitude = earthquakeInfo.Data.Magnitude;

            OnPropertyChanged(nameof(PlaceName));
            OnPropertyChanged(nameof(Updates));
            OnPropertyChanged(nameof(Magnitude));
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(LocalIntensity));
        };
        timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

