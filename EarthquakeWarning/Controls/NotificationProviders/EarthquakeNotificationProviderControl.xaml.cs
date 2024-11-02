using EarthquakeWarning.Calculators;
using EarthquakeWarning.Services.NotificationProviders;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
namespace EarthquakeWarning.Controls.NotificationProviders;
public partial class EarthquakeNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    private object? _element;
    public EarthquakeReport EarthquakeReport { get; }
    public string LocalIntensity 
    {
        get
        {
            return LocalIntensityCalculator.CalculateLocalIntensity(EarthquakeReport, LocalPosition).ToString("F1");
        }
    }
    public string SWaveArrivalTime 
    {
        get
        {
            return Math.Round(SWaveArrivalTimeCalculater.CalculateSWaveArrivalTime(EarthquakeReport, LocalPosition)).ToString();
        }
    }
    public TimeSpan time 
    {
        get 
        {
            TimeSpan timeDifference = DateTime.Parse(EarthquakeReport.OriginTime).AddSeconds(int.Parse(SWaveArrivalTime)) - DateTime.Now;
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
    public EarthquakeNotificationProviderControl(string key,EarthquakeReport earthquakeReport, LocalPosition localPosition)
    {
        LocalPosition = localPosition;
        EarthquakeReport = earthquakeReport;
        InitializeComponent();
        var visual = FindResource(key) as FrameworkElement;
        Element = visual;
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            OnPropertyChanged(nameof(time));
            OnPropertyChanged(nameof(EarthquakeReport));
            OnPropertyChanged(nameof(LocalIntensity));
            OnPropertyChanged(nameof(SWaveArrivalTime));
        };
        timer.Start();
    }
}
