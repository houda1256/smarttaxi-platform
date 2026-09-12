using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record SharedRideMatchSuggested(Guid MatchId, DateTime OccurredAtUtc) : IDomainEvent;
