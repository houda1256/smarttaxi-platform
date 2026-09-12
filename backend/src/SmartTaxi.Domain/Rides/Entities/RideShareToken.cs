using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Events;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>Only a SHA-256 hash of the token is ever persisted — the raw token is never stored. Revoke is an atomic repository-level guard.</summary>
public sealed class RideShareToken : AggregateRoot
{
    public Guid RideId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RideShareToken()
    {
    }

    private RideShareToken(Guid rideId, string tokenHash, DateTime utcNow, TimeSpan validity)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        TokenHash = tokenHash;
        CreatedAt = utcNow;
        ExpiresAt = utcNow.Add(validity);

        RaiseDomainEvent(new RideShareLinkCreated(Id, rideId, utcNow));
    }

    public static RideShareToken Create(Guid rideId, string tokenHash, DateTime utcNow, TimeSpan validity)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Le hash du jeton est requis.");
        }

        if (validity <= TimeSpan.Zero)
        {
            throw new ArgumentException("La durée de validité doit être positive.");
        }

        return new RideShareToken(rideId, tokenHash, utcNow, validity);
    }

    public bool IsValid(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;
}
