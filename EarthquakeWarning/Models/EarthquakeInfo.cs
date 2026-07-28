namespace EarthquakeWarning.Models;

public class EarthquakeInfo
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public int Updates { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Depth { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public string ShockTime { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
    public double Magnitude { get; set; }
    public string Province { get; set; } = string.Empty;
    public double EpiIntensity { get; set; }

    public void UpdateFrom(EarthquakeInfo info)
    {
        Id = info.Id;
        EventId = info.EventId;
        Updates = info.Updates;
        Latitude = info.Latitude;
        Longitude = info.Longitude;
        Depth = info.Depth;
        PlaceName = info.PlaceName;
        ShockTime = info.ShockTime;
        UpdateTime = info.UpdateTime;
        Magnitude = info.Magnitude;
        Province = info.Province;
        EpiIntensity = info.EpiIntensity;
    }
}
