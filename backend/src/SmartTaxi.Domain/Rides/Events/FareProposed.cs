using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record FareProposed(Guid ProposalId, Guid RideId, Guid ProposedBy, decimal Amount, DateTime OccurredAtUtc) : IDomainEvent;
