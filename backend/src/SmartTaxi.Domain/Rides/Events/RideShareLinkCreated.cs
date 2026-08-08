using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record RideShareLinkCreated(Guid TokenId, Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;
