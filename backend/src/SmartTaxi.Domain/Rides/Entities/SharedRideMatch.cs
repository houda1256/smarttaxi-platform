using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.Events;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// The two paired Rides are tracked via separate SharedRideParticipant rows
/// (their own table), not an owned collection here — each participant's
/// approval is its own atomically-guarded row, and the Driver's approval is
/// tracked on this aggregate directly. Confirm/Reject/Expire/Cancel are
/// atomic repository-level transitions, not domain methods.
/// </summary>
public sealed class SharedRideMatch : AggregateRoot
{
    public SharedRideMatchStatus Status { get; private set; }
    public Guid? DriverId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private SharedRideMatch()
    {
    }

    private SharedRideMatch(DateTime utcNow, TimeSpan expiry)
        : base(Guid.NewGuid())
    {
        Status = SharedRideMatchStatus.Matching;
        CreatedAt = utcNow;
        ExpiresAt = utcNow.Add(expiry);

        RaiseDomainEvent(new SharedRideMatchSuggested(Id, utcNow));
    }

    public static SharedRideMatch Suggest(DateTime utcNow, TimeSpan expiry)
    {
        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentException("Le délai d'expiration doit être positif.");
        }

        return new SharedRideMatch(utcNow, expiry);
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
}
