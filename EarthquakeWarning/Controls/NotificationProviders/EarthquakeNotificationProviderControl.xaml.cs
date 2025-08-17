using EarthquakeWarning.Calculators;
using EarthquakeWarning.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
namespace EarthquakeWarning.Controls.NotificationProviders;
public partial class EarthquakeNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    public EarthquakeInfo EarthquakeInfo { get; set; }

    public string LocalIntensity 
    {
        get
        {
            double distance = HuaniaEarthQuakeCalculator.GetDistance(_localPosition.Latitude, _localPosition.Longitude, EarthquakeInfo.Data.Latitude, EarthquakeInfo.Data.Longitude);
            return HuaniaEarthQuakeCalculator.GetIntensity(double.Parse(EarthquakeInfo.Data.Magnitude), distance).ToString("F1");
        }
    }

    public string SWaveArrivalTime 
    {
        get
        {
            double distance = HuaniaEarthQuakeCalculator.GetDistance(_localPosition.Latitude, _localPosition.Longitude, EarthquakeInfo.Data.Latitude, EarthquakeInfo.Data.Longitude);
            return ((int)HuaniaEarthQuakeCalculator.GetCountDownSeconds(EarthquakeInfo.Data.Depth, distance)).ToString();
        }
    }

    public TimeSpan Time 
    {
        get 
        {
            TimeSpan timeDifference = DateTime.Parse(EarthquakeInfo.Data.ShockTime).AddSeconds(int.Parse(SWaveArrivalTime)) - DateTime.Now;
            return timeDifference;
        }
        set { }
    }

    public object? Element
    {
        get => _element;
        set
        {
            if (Equals(value, _element)) return;
            _element = value;
            OnPropertyChanged();
        }
    }

    private object? _element;

    private readonly LocalPosition _localPosition;

    public EarthquakeNotificationProviderControl(EarthquakeInfo earthquakeInfo, LocalPosition localPosition)
    {
        this._localPosition = localPosition;
        EarthquakeInfo = earthquakeInfo;
        InitializeComponent();
        var visual = FindResource("EarthquakeNotifyOverlay") as FrameworkElement;
        Element = visual;
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            OnPropertyChanged(nameof(EarthquakeInfo));
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(LocalIntensity));
            OnPropertyChanged(nameof(SWaveArrivalTime));
        };
        timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
