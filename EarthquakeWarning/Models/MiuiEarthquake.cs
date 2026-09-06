using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthquakeWarning.Models;

public sealed class MiuiEarthquakeResponse
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("desc")] public string? Description { get; set; }
    [JsonPropertyName("data")] public List<MiuiEarthquakeRecord>? Data { get; set; }
}

public sealed class MiuiEarthquakeRecord
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("eventId")] public long EventId { get; set; }
    [JsonPropertyName("update")] public int Update { get; set; }
    [JsonPropertyName("startAt")] public long StartAt { get; set; }
    [JsonPropertyName("updateAt")] public long UpdateAt { get; set; }
    [JsonPropertyName("magnitude")] public double Magnitude { get; set; }
    [JsonPropertyName("depth")] public double Depth { get; set; }
    [JsonPropertyName("longitude")] public double Longitude { get; set; }
    [JsonPropertyName("latitude")] public double Latitude { get; set; }
    [JsonPropertyName("epicenter")] public string? Epicenter { get; set; }

    public EarthquakeInfo ToEarthquakeInfo()
    {
        return new EarthquakeInfo
        {
            Id = EventId.ToString(),
            EventId = EventId.ToString(),
            Updates = Update,
            Latitude = Latitude,
            Longitude = Longitude,
            Depth = Depth,
            PlaceName = Epicenter ?? string.Empty,
            ShockTime = EarthquakeTime.Format(DateTimeOffset.FromUnixTimeMilliseconds(StartAt).LocalDateTime),
            UpdateTime = EarthquakeTime.Format(DateTimeOffset.FromUnixTimeMilliseconds(UpdateAt).LocalDateTime),
            Magnitude = Magnitude,
        };
    }
}

public static class MiuiEarthquakeSigning
{
    private const string SigningKeyBase64 = "N2h0cjUyMzgtYThjZi0zazc5LWVjNzMtNzUzODIxNDVuczVj";

    public static string MakeSignature(IReadOnlyDictionary<string, string> parameters)
    {
        var canonical = string.Join("&", parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
        var signingKey = Encoding.UTF8.GetString(Convert.FromBase64String(SigningKeyBase64));
        var payload = Encoding.UTF8.GetBytes($"{canonical}&{signingKey}");
        var encoded = Convert.ToBase64String(payload);
        return Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(encoded)));
    }
}
