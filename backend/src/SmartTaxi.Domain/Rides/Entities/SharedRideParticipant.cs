using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>One row per Ride participating in a SharedRideMatch — approval is an atomic repository-level guard.</summary>
public sealed class SharedRideParticipant
{
    public Guid Id { get; private set; }
    public Guid SharedRideMatchId { get; private set; }
    public Guid RideId { get; private set; }
    public Guid CustomerId { get; private set; }
    public SharedRideParticipantApprovalStatus ApprovalStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    private SharedRideParticipant()
    {
    }

    public SharedRideParticipant(Guid sharedRideMatchId, Guid rideId, Guid customerId, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        SharedRideMatchId = sharedRideMatchId;
        RideId = rideId;
        CustomerId = customerId;
        ApprovalStatus = SharedRideParticipantApprovalStatus.Pending;
        CreatedAt = utcNow;
    }
}
