using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

/// <summary>Documentation only — revocation is an atomic transition on an existing token row.</summary>
public sealed record RideShareLinkRevoked(Guid TokenId, Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;
