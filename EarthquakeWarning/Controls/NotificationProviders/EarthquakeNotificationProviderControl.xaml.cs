using EarthquakeWarning.Models.EarthquakeModels;
using EarthquakeWarning.Services;
using EarthquakeWarning.Services.NotificationProviders;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
namespace EarthquakeWarning.Controls.NotificationProviders;
public partial class EarthquakeNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    private object? _element;
    public EarthquakeInfoBase EarthquakeInfo { get; }
    private HuaniaEarthQuakeCalculator EarthquakeCalculator = new();
    public string LocalIntensity 
    {
        get
        {
            double distance = EarthquakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, EarthquakeInfo.Latitude, EarthquakeInfo.Longitude);
            return EarthquakeCalculator.GetIntensity(EarthquakeInfo.Magnitude, distance).ToString("F1");
        }
    }
    public string SWaveArrivalTime 
    {
        get
        {
            double distance = EarthquakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, EarthquakeInfo.Latitude, EarthquakeInfo.Longitude);
            return ((int)EarthquakeCalculator.GetCountDownSeconds(EarthquakeInfo.Depth,distance)).ToString();
        }
    }
    public TimeSpan time 
    {
        get 
        {
            TimeSpan timeDifference = EarthquakeInfo.StartAt.AddSeconds(int.Parse(SWaveArrivalTime)) - DateTime.Now;
            return timeDifference;
        }
        set { }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
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
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public LocalPosition LocalPosition;
    public EarthquakeNotificationProviderControl(string key, EarthquakeInfoBase earthquakeInfo, LocalPosition localPosition)
    {
        LocalPosition = localPosition;
        EarthquakeInfo = earthquakeInfo;
        InitializeComponent();
        var visual = FindResource(key) as FrameworkElement;
        Element = visual;
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            OnPropertyChanged(nameof(time));
            OnPropertyChanged(nameof(EarthquakeInfo));
            OnPropertyChanged(nameof(LocalIntensity));
            OnPropertyChanged(nameof(SWaveArrivalTime));
        };
        timer.Start();
    }
}
