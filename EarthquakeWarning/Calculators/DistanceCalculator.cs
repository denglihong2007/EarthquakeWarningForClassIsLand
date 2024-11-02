using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthquakeWarning.Calculators
{
    internal class DistanceCalculator
    {
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = RadiansCalculator.CalculateRadians(lat2 - lat1);
            double dLon = RadiansCalculator.CalculateRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(RadiansCalculator.CalculateRadians(lat1)) * Math.Cos(RadiansCalculator.CalculateRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return 6371 * c;
        }
    }
}
