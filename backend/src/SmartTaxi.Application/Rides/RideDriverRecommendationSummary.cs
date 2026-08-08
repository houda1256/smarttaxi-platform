namespace SmartTaxi.Application.Rides;

/// <summary>
/// DriverFirstName/ProfilePictureReference map onto Identity's existing
/// UserPreferences.DisplayName/AvatarUrl (Identity 2e) — there is no separate
/// "name" field anywhere in Identity or Fleet today, so this reuses what
/// already exists rather than adding a new one. Both are null if the Driver
/// never set preferences.
/// </summary>
public sealed record RideDriverRecommendationSummary(
    Guid DriverId,
    Guid VehicleId,
    string? DriverDisplayName,
    string? DriverProfilePictureReference,
    decimal AverageRating,
    int CompletedRideCount,
    decimal DistanceToPickupKm,
    int EstimatedArrivalMinutes,
    string VehicleBrand,
    string VehicleModel,
    string VehicleCategory,
    string LicensePlate,
    bool HasAirConditioning,
    bool IsAccessible,
    int SeatCount,
    decimal EstimatedFare,
    decimal RecommendationScore,
    IReadOnlyCollection<string> RecommendationReasons,
    int Rank);
