using System.Diagnostics.CodeAnalysis;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.ValueObjects;

public sealed class GeoCoordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    private GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoCoordinate Create(double latitude, double longitude)
    {
        if (!TryCreate(latitude, longitude, out var coordinate, out var error))
        {
            throw new ArgumentException(error);
        }

        return coordinate;
    }

    public static bool TryCreate(
        double latitude, double longitude, [NotNullWhen(true)] out GeoCoordinate? coordinate,
        [NotNullWhen(false)] out string? error)
    {
        if (latitude is < -90 or > 90)
        {
            coordinate = null;
            error = "La latitude doit être comprise entre -90 et 90.";
            return false;
        }

        if (longitude is < -180 or > 180)
        {
            coordinate = null;
            error = "La longitude doit être comprise entre -180 et 180.";
            return false;
        }

        coordinate = new GeoCoordinate(latitude, longitude);
        error = null;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
