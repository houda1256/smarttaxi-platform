using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>Immutable, append-only — written by the same atomic call that performs the transition it records.</summary>
public sealed class RideStatusHistory
{
    public Guid Id { get; private set; }
    public Guid RideId { get; private set; }
    public RideStatus? PreviousStatus { get; private set; }
    public RideStatus NewStatus { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private RideStatusHistory()
    {
    }

    public RideStatusHistory(
        Guid rideId, RideStatus? previousStatus, RideStatus newStatus, Guid? changedBy, string? reason, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        RideId = rideId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        Reason = reason;
        ChangedAt = utcNow;
    }
}
