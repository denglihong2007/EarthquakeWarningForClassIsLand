using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace EarthquakeWarning.Converters
{
    internal class IntensityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && double.TryParse(strValue, out double intensity))
            {
                if (intensity <= 2)
                    return Brushes.Blue;
                else if (intensity >= 2 && intensity <= 4)
                    return Brushes.Yellow;
                else if (intensity >= 4 && intensity <= 6)
                    return Brushes.Orange;
                else
                    return Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
