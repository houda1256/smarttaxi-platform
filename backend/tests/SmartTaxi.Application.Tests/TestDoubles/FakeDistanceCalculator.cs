using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Deterministic stand-in for HaversineDistanceCalculator — a simple flat-plane approximation is more than sufficient for unit-testing distance-ordering/nullability behavior without pulling in the real Infrastructure implementation.</summary>
public sealed class FakeDistanceCalculator : IDistanceCalculator
{
    public decimal CalculateKilometers(GeoCoordinate from, GeoCoordinate to)
    {
        var latDelta = from.Latitude - to.Latitude;
        var lonDelta = from.Longitude - to.Longitude;
        return (decimal)Math.Sqrt(latDelta * latDelta + lonDelta * lonDelta) * 111m;
    }
}
