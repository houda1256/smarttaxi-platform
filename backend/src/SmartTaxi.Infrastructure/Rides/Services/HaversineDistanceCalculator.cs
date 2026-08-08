using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Infrastructure.Rides.Services;

/// <summary>No real map/routing provider is integrated — great-circle distance is a documented, deterministic approximation for development.</summary>
internal sealed class HaversineDistanceCalculator : IDistanceCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public decimal CalculateKilometers(GeoCoordinate from, GeoCoordinate to)
    {
        var lat1 = DegreesToRadians(from.Latitude);
        var lat2 = DegreesToRadians(to.Latitude);
        var deltaLat = DegreesToRadians(to.Latitude - from.Latitude);
        var deltaLng = DegreesToRadians(to.Longitude - from.Longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (decimal)(EarthRadiusKm * c);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
