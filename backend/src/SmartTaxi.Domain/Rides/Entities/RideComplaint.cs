using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.Events;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// Self-contained — no Support Ticket module exists yet to integrate with.
/// A future Support module can migrate/link these once it exists; this
/// entity carries the complete required field list on its own in the
/// meantime, per the master prompt's instruction not to fabricate an
/// integration with infrastructure that doesn't exist.
/// </summary>
public sealed class RideComplaint : AggregateRoot
{
    public Guid RideId { get; private set; }
    public Guid ComplainantUserId { get; private set; }
    public Guid ConcernedUserId { get; private set; }
    public RideComplaintCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public RideComplaintStatus Status { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private RideComplaint()
    {
    }

    private RideComplaint(
        Guid rideId, Guid complainantUserId, Guid concernedUserId, RideComplaintCategory category, string description,
        DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        ComplainantUserId = complainantUserId;
        ConcernedUserId = concernedUserId;
        Category = category;
        Description = description;
        Status = RideComplaintStatus.Open;
        CreatedAt = utcNow;

        RaiseDomainEvent(new RideComplaintSubmitted(Id, rideId, complainantUserId, utcNow));
    }

    public static RideComplaint Submit(
        Guid rideId, Guid complainantUserId, Guid concernedUserId, RideComplaintCategory category, string description,
        DateTime utcNow)
    {
        if (complainantUserId == concernedUserId)
        {
            throw new ArgumentException("Le plaignant et la partie concernée doivent être différents.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Une description est requise.");
        }

        return new RideComplaint(rideId, complainantUserId, concernedUserId, category, description, utcNow);
    }
}
