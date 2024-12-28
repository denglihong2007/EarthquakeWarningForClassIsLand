using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace EarthquakeWarning.Models.EarthquakeModels;

public partial class EarthquakeInfoBase : ObservableRecipient
{
    public string Id { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Magnitude { get; set; }
    public double Depth { get; set; }
    public string? PlaceName { get; set; }
    public event Action<EarthquakeInfoBase> ReportUpdated;
    protected virtual void OnReportUpdated()
    {
        ReportUpdated?.Invoke(this);
    }
    public void UpdateInfo(EarthquakeInfoBase earthquakeInfoBase)
    {
        Id = earthquakeInfoBase.Id;
        StartAt = earthquakeInfoBase.StartAt;
        UpdateAt = earthquakeInfoBase.UpdateAt;
        Latitude = earthquakeInfoBase.Latitude;
        Longitude = earthquakeInfoBase.Longitude;
        Magnitude = earthquakeInfoBase.Magnitude;
        Depth = earthquakeInfoBase.Depth;
        PlaceName = earthquakeInfoBase.PlaceName;
        OnReportUpdated();
    }
}
