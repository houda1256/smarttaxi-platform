using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRouteEstimationService
{
    RouteEstimate Estimate(GeoCoordinate from, GeoCoordinate to);
}

public sealed record RouteEstimate(decimal DistanceKm, int DurationMinutes);
