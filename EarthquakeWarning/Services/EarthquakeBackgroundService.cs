using EarthquakeWarning.Models;

namespace EarthquakeWarning.Services
{
    public class SharedService
    {
        public bool Testing = false;
        public event EventHandler<EarthquakeInfo>? TestInfoUpdated;
        public async Task Example()
        {
            Testing = true;
            DateTime StartTime = DateTime.Now;
            int eventId = 0;
            for (int i = 0; i < 6; i++)
            {
                TestInfoUpdated?.Invoke(this, new EarthquakeInfo
                {
                    Type = "Earthquake",
                    Data = new Data
                    {
                        EventId = eventId + i,
                        Updates = i + 1,
                        Latitude = 31.0,
                        Longitude = 103.4,
                        Depth = 14,
                        PlaceName = "四川省阿坝藏族羌族自治州汶川县",
                        ShockTime = StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Magnitude = (4.0 + (i + 1.0) * 0.8).ToString("F1"),
                        InsideNet = 43,
                        Sations = 43,
                        SourceType = "Huania",
                        EpiIntensity = 12
                    },
                    Md5 = "example-md5-hash-" + (eventId + i)
                });
                await Task.Delay(10000);
            }
            Testing = false;
        }

    }
}
