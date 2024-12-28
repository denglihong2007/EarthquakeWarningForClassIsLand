namespace EarthquakeWarning.Services;

public interface IEarthquakeCalculator
{
    double GetIntensity(double magnitude, double distance);
    double GetCountDownSeconds(double depth, double distance);
    double GetDistance(double latitude1, double longitude1, double latitude2, double longitude2);
}