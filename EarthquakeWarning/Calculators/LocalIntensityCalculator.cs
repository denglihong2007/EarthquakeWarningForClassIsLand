using EarthquakeWarning.Services.NotificationProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthquakeWarning.Calculators
{
    internal class LocalIntensityCalculator
    {
        public static double CalculateLocalIntensity(EarthquakeReport earthquakeReport, LocalPosition localPosition)
        {
            double distance = DistanceCalculator.CalculateDistance(earthquakeReport.Latitude, earthquakeReport.Longitude, localPosition.Latitude, localPosition.Longitude);
            return 0.92 + 1.63 * earthquakeReport.Magunitude - 3.49 * Math.Log10(distance);
        }
    }
}
