using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Deterministic: distance is the Euclidean distance in degrees scaled to km (good enough for test fixtures), duration assumes 30 km/h.</summary>
public sealed class FakeRouteEstimationService : IRouteEstimationService
{
    public RouteEstimate Estimate(GeoCoordinate from, GeoCoordinate to)
    {
        var latDelta = to.Latitude - from.Latitude;
        var lngDelta = to.Longitude - from.Longitude;
        var distanceKm = (decimal)(Math.Sqrt(latDelta * latDelta + lngDelta * lngDelta) * 111);
        var durationMinutes = (int)Math.Ceiling((double)distanceKm / 30 * 60);

        return new RouteEstimate(Math.Round(distanceKm, 2), Math.Max(durationMinutes, 1));
    }
}
