using EarthquakeWarning.Converters;
using System.Text.Json.Serialization;

namespace EarthquakeWarning.Models;

public class Data
{
    public int EventId { get; set; }
    public int Updates { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Depth { get; set; }
    public string PlaceName { get; set; }
    public string ShockTime { get; set; }
    public string UpdateTime { get; set; }
    public string Magnitude { get; set; }
    public int InsideNet { get; set; }
    public int Sations { get; set; }
    public string SourceType { get; set; }
    public int EpiIntensity { get; set; }
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
