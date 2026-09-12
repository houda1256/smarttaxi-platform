using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record FareCounterProposed(
    Guid ProposalId, Guid RideId, Guid ProposedBy, decimal Amount, int RoundNumber, DateTime OccurredAtUtc) : IDomainEvent;
