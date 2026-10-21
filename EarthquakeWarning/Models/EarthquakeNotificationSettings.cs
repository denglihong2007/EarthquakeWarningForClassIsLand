using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

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
    private bool _isSpeechEnabled = true;
    [ObservableProperty]
    private double _speechThreshold = 2.0;
    [ObservableProperty]
    private string _info = "";
    [ObservableProperty]
    private EarthquakeInfo earthquakeInfo = new();
    [ObservableProperty]
    private string _serverInfo = "";
    [ObservableProperty]
    private bool _isCommandEnabled;
    [ObservableProperty]
    private string _command = "";
    [ObservableProperty]
    private string _actionStatus = "尚未执行预警动作";
    /// <summary>
    /// 预警演练期间为 true，用于阻止真实预警动作（如关机命令）被执行。
    /// </summary>
    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isDrillMode;
}
