using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record RideRequested(Guid RideId, Guid CustomerId, DateTime OccurredAtUtc) : IDomainEvent;
