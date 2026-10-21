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
    public string PlaceName { get; set; } = string.Empty;
    public int Distance { get; set; }
    public int Updates { get; set; }
    public double Magnitude { get; set; }

    public string LocalIntensity 
    {
        get
        {
            double distance = HuaniaEarthQuakeCalculator.GetDistance(Latitude, Longitude, EarthquakeInfo.Latitude, EarthquakeInfo.Longitude);
            return HuaniaEarthQuakeCalculator
                .GetIntensity(EarthquakeInfo.Magnitude, distance, EarthquakeInfo.Depth, EarthquakeInfo.Longitude)
                .ToString("F1");
        }
    }


    public TimeSpan Time 
    {
        get 
        {
            double distance = HuaniaEarthQuakeCalculator.GetDistance(Latitude, Longitude, EarthquakeInfo.Latitude, EarthquakeInfo.Longitude);
            double depth = EarthquakeInfo.Depth is > 0 ? EarthquakeInfo.Depth.Value : 10.0;
            double expectTime = HuaniaEarthQuakeCalculator.GetCountDownSeconds(depth, distance);
            DateTime pWaveArriveTime = EarthquakeTime.Parse(EarthquakeInfo.ShockTime).AddSeconds(expectTime);
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
            PlaceName = earthquakeInfo.PlaceName;
            Updates = earthquakeInfo.Updates;
            Distance = (int)Math.Round(HuaniaEarthQuakeCalculator.GetDistance(Latitude, Longitude, earthquakeInfo.Latitude, earthquakeInfo.Longitude));
            Magnitude = earthquakeInfo.Magnitude;

            OnPropertyChanged(nameof(PlaceName));
            OnPropertyChanged(nameof(Updates));
            OnPropertyChanged(nameof(Distance));
            OnPropertyChanged(nameof(Magnitude));
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(LocalIntensity));
        };
        timer.Start();
        // 提醒关闭后控件会离开可视化树，必须停止计时器，否则会一直持有控件造成泄漏。
        DetachedFromVisualTree += (_, _) => timer.Stop();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

