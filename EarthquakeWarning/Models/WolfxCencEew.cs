using System.Text.Json.Serialization;

namespace EarthquakeWarning.Models;

public class WolfxCencEew
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("EventID")]
    public string EventId { get; set; } = string.Empty;

    public string ReportTime { get; set; } = string.Empty;
    public int ReportNum { get; set; }
    public string OriginTime { get; set; } = string.Empty;
    public string HypoCenter { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Magnitude { get; set; }
    public double? Depth { get; set; }
    public double MaxIntensity { get; set; }

    public EarthquakeInfo ToEarthquakeInfo()
    {
        return new EarthquakeInfo
        {
            Id = Id,
            EventId = EventId,
            Updates = ReportNum,
            Latitude = Latitude,
            Longitude = Longitude,
            Depth = Depth,
            PlaceName = HypoCenter,
            ShockTime = OriginTime,
            UpdateTime = ReportTime,
            Magnitude = Magnitude,
            Province = string.Empty,
            EpiIntensity = MaxIntensity,
        };
    }
}
