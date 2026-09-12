using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

/// <summary>Documentation only — a Ride's Status moving to Disputed is an atomic repository-level transition, never a domain mutation.</summary>
public sealed record RideDisputed(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;
