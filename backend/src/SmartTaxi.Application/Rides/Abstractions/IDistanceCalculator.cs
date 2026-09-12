using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>No real map/routing provider is integrated — the development implementation uses Haversine great-circle distance.</summary>
public interface IDistanceCalculator
{
    decimal CalculateKilometers(GeoCoordinate from, GeoCoordinate to);
}
