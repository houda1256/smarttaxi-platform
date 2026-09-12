using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Fleets.Events;

public sealed record FleetCreated(Guid FleetId, Guid OwnerId, DateTime OccurredAtUtc) : IDomainEvent;
