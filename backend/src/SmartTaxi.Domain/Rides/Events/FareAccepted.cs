using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

/// <summary>
/// Defined for documentation only — acceptance is an atomic repository-level
/// transition on an existing proposal row, not a mutation this entity
/// performs, so it is never actually raised (same split as every Fleet
/// status-transition event).
/// </summary>
public sealed record FareAccepted(Guid ProposalId, Guid RideId, decimal Amount, DateTime OccurredAtUtc) : IDomainEvent;
