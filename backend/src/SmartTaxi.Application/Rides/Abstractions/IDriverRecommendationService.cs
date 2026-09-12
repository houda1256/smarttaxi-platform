using SmartTaxi.Application.Fleet.Drivers;
using SmartTaxi.Application.Fleet.Vehicles;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>
/// Explainable, weighted, deterministic — never ML. A subscription-ranking
/// bonus may be added on top of the score but must never override distance,
/// availability, compatibility, safety, or document validity, all of which
/// are eligibility gates applied before scoring even begins.
/// </summary>
public interface IDriverRecommendationService
{
    Task<IReadOnlyCollection<DriverRecommendationResult>> GetRecommendationsAsync(
        DriverRecommendationCriteria criteria, CancellationToken cancellationToken);
}

public sealed record DriverRecommendationCriteria(
    GeoCoordinate PickupLocation,
    VehicleCategory? PreferredVehicleCategory,
    int PassengerCount,
    bool NeedsAccessibleVehicle,
    bool NeedsAirConditioning);

public sealed record DriverRecommendationResult(
    Guid DriverId,
    Guid VehicleId,
    DriverProfileSummary Driver,
    VehicleSummary Vehicle,
    decimal DistanceToPickupKm,
    int EstimatedArrivalMinutes,
    decimal RecommendationScore,
    IReadOnlyCollection<string> RecommendationReasons);
