using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthquakeWarning.Calculators
{
    internal class RadiansCalculator
    {
        public static double CalculateRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
