using EarthquakeWarning.Converters;
using System.Text.Json.Serialization;

namespace EarthquakeWarning.Models;

public class Data
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public int Updates { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Depth { get; set; }
    public string PlaceName { get; set; }
    public string ShockTime { get; set; }
    public string UpdateTime { get; set; }
    public double Magnitude { get; set; }
    public string Province { get; set; }
    public double EpiIntensity { get; set; }
}

public class EarthquakeInfo
{
    public string Type { get; set; }
    [JsonPropertyName("Data")]
    public Data Data { get; set; }
    public string Md5 { get; set; }
    public void UpdateFrom(EarthquakeInfo info)
    {
        Type = info.Type;
        Data = info.Data;
        Md5 = info.Md5;
    }
}
