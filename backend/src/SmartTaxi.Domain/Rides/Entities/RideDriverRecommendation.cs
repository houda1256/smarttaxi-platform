namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// A persisted snapshot of one search's ranked results — read back verbatim
/// by GetRecommendedDrivers rather than recomputed on every read, so the list
/// a Customer sees stays stable between the search call and their selection.
/// </summary>
public sealed class RideDriverRecommendation
{
    public Guid Id { get; private set; }
    public Guid RideId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public decimal DistanceToPickupKm { get; private set; }
    public int EstimatedArrivalMinutes { get; private set; }
    public decimal RecommendationScore { get; private set; }
    public string RecommendationReasons { get; private set; } = string.Empty;
    public int Rank { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RideDriverRecommendation()
    {
    }

    public RideDriverRecommendation(
        Guid rideId, Guid driverId, Guid vehicleId, decimal distanceToPickupKm, int estimatedArrivalMinutes,
        decimal recommendationScore, string recommendationReasons, int rank, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        RideId = rideId;
        DriverId = driverId;
        VehicleId = vehicleId;
        DistanceToPickupKm = distanceToPickupKm;
        EstimatedArrivalMinutes = estimatedArrivalMinutes;
        RecommendationScore = recommendationScore;
        RecommendationReasons = recommendationReasons;
        Rank = rank;
        CreatedAt = utcNow;
    }
}
