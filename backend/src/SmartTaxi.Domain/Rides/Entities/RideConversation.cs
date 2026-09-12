using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>ReadOnly/Archived transitions are atomic repository-level guards, applied after a configurable delay post-completion/cancellation.</summary>
public sealed class RideConversation : AggregateRoot
{
    public Guid RideId { get; private set; }
    public RideConversationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadOnlyAt { get; private set; }

    private RideConversation()
    {
    }

    private RideConversation(Guid rideId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        Status = RideConversationStatus.Active;
        CreatedAt = utcNow;
    }

    public static RideConversation CreateForRide(Guid rideId, DateTime utcNow) => new(rideId, utcNow);
}
