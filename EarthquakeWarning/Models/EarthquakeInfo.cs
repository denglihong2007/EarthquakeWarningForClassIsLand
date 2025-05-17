using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace EarthquakeWarning.Models.EarthquakeModels;

public partial class EarthquakeInfo : ObservableRecipient
{
    public int ID { get; set; }
    public string EventID { get; set; }
    public DateTime ReportTime { get; set; }
    public int ReportNum { get; set; }
    public DateTime OriginTime { get; set; }
    public string? HypoCenter { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Magunitude { get; set; }
    public double? Depth { get; set; }
    public double MaxIntensity { get; set; }
    public string? Pond { get; set; }
    public event Action<EarthquakeInfo> ReportUpdated;
    protected virtual void OnReportUpdated()
    {
        ReportUpdated?.Invoke(this);
    }
    public void UpdateInfo(EarthquakeInfo earthquakeInfoBase)
    {
        if(EventID == earthquakeInfoBase.EventID)
        {
            return;
        }
        ID = earthquakeInfoBase.ID;
        EventID = earthquakeInfoBase.EventID;
        ReportTime = earthquakeInfoBase.ReportTime;
        ReportNum = earthquakeInfoBase.ReportNum;
        OriginTime = earthquakeInfoBase.OriginTime;
        HypoCenter = earthquakeInfoBase.HypoCenter;
        Latitude = earthquakeInfoBase.Latitude;
        Longitude = earthquakeInfoBase.Longitude;
        Magunitude = earthquakeInfoBase.Magunitude;
        Depth = earthquakeInfoBase.Depth;
        MaxIntensity = earthquakeInfoBase.MaxIntensity;
        Pond = earthquakeInfoBase.Pond;
        OnReportUpdated();
    }
}
