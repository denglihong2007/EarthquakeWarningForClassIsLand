using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace EarthquakeWarning.Converters
{
    internal class IntensityToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (TryGetIntensity(value, culture, out var intensity))
            {
                return new SolidColorBrush(GetColor(intensity));
            }

            return Brushes.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static Color GetColor(double intensity)
        {
            if (intensity <= 2)
                return Colors.Blue;
            if (intensity <= 4)
                return Colors.Yellow;
            if (intensity <= 6)
                return Colors.Orange;
            return Colors.Red;
        }

        private static bool TryGetIntensity(object? value, CultureInfo culture, out double intensity)
        {
            intensity = 0;

            if (value is double doubleValue)
            {
                intensity = doubleValue;
                return true;
            }

            return value is string stringValue &&
                   double.TryParse(stringValue, NumberStyles.Float, culture, out intensity);
        }
    }
}
