using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

/// <summary>Documentation only — confirmation is an atomic transition once both Customers and the Driver have approved.</summary>
public sealed record SharedRideConfirmed(Guid MatchId, DateTime OccurredAtUtc) : IDomainEvent;
