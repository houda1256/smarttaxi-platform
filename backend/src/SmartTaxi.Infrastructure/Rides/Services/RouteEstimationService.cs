using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Infrastructure.Rides.Services;

/// <summary>Duration is derived from Haversine distance at an assumed average urban speed — a documented dev-time approximation, not a real routing engine.</summary>
internal sealed class RouteEstimationService : IRouteEstimationService
{
    private const decimal AssumedAverageSpeedKmh = 30m;

    private readonly IDistanceCalculator _distanceCalculator;

    public RouteEstimationService(IDistanceCalculator distanceCalculator)
    {
        _distanceCalculator = distanceCalculator;
    }

    public RouteEstimate Estimate(GeoCoordinate from, GeoCoordinate to)
    {
        var distanceKm = _distanceCalculator.CalculateKilometers(from, to);
        var durationMinutes = (int)Math.Ceiling(distanceKm / AssumedAverageSpeedKmh * 60);

        return new RouteEstimate(Math.Round(distanceKm, 2), Math.Max(durationMinutes, 1));
    }
}
