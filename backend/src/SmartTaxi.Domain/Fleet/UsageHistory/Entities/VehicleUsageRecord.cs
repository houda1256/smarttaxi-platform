namespace SmartTaxi.Domain.Fleet.UsageHistory.Entities;

/// <summary>
/// Immutable, append-only history — no update or delete methods are exposed
/// deliberately. A record is written once the usage period is fully known
/// (e.g. when an assignment completes).
/// </summary>
public sealed class VehicleUsageRecord
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime EndedAt { get; private set; }
    public int MileageStart { get; private set; }
    public int MileageEnd { get; private set; }
    public int RideCount { get; private set; }
    public decimal? RevenueGenerated { get; private set; }
    public int IncidentCount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private VehicleUsageRecord()
    {
    }

    public VehicleUsageRecord(
        Guid vehicleId, Guid driverId, Guid assignmentId, DateTime startedAt, DateTime endedAt,
        int mileageStart, int mileageEnd, int rideCount, decimal? revenueGenerated, int incidentCount, DateTime utcNow)
    {
        if (endedAt < startedAt)
        {
            throw new ArgumentException("La date de fin ne peut pas précéder la date de début.");
        }

        if (mileageEnd < mileageStart)
        {
            throw new ArgumentException("Le kilométrage de fin ne peut pas être inférieur au kilométrage de début.");
        }

        Id = Guid.NewGuid();
        VehicleId = vehicleId;
        DriverId = driverId;
        AssignmentId = assignmentId;
        StartedAt = startedAt;
        EndedAt = endedAt;
        MileageStart = mileageStart;
        MileageEnd = mileageEnd;
        RideCount = rideCount;
        RevenueGenerated = revenueGenerated;
        IncidentCount = incidentCount;
        CreatedAt = utcNow;
    }
}
