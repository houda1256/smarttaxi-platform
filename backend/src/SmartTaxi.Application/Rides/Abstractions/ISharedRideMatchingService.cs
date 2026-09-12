using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>Deterministic, rule-based compatibility check — never ML.</summary>
public interface ISharedRideMatchingService
{
    SharedRideCompatibilityReport CheckCompatibility(SharedRideCompatibilityInput input);
}

public sealed record SharedRideCompatibilityInput(
    GeoCoordinate PickupA,
    GeoCoordinate PickupB,
    GeoCoordinate DestinationA,
    GeoCoordinate DestinationB,
    DateTime RequestedAtA,
    DateTime RequestedAtB,
    int PassengerCountA,
    int PassengerCountB,
    int VehicleSeatCount);

public sealed record SharedRideCompatibilityReport(bool IsCompatible, IReadOnlyCollection<string> Reasons);
