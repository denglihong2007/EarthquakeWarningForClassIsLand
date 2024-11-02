using EarthquakeWarning.Services.NotificationProviders;

namespace EarthquakeWarning.Calculators
{
    internal class SWaveArrivalTimeCalculater
    {
        public static double CalculateSWaveArrivalTime(EarthquakeReport earthquakeReport, LocalPosition localPosition)
        {
            double distance = DistanceCalculator.CalculateDistance(earthquakeReport.Latitude, earthquakeReport.Longitude, localPosition.Latitude, localPosition.Longitude);
            return distance / 4.0; // 计算到达时间 (秒)
        }
    }
}
