using System.Globalization;

namespace EarthquakeWarning.Models;

public static class EarthquakeTime
{
    private const string StandardFormat = "yyyy-MM-dd HH:mm:ss";
    private static readonly string[] SupportedFormats =
    {
        StandardFormat,
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH'时'mm'时'ss",
        "yyyy-MM-dd HH'时'mm'分'ss'秒'",
    };

    public static string Format(DateTime value)
    {
        return value.ToString(StandardFormat, CultureInfo.InvariantCulture);
    }

    public static DateTime Parse(string value)
    {
        if (DateTime.TryParseExact(
                value,
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out var result))
        {
            return result;
        }

        throw new FormatException($"无法识别地震预警时间：{value}");
    }
}
