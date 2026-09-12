using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Fleets.Events;

public sealed record FleetSuspended(Guid FleetId, DateTime OccurredAtUtc) : IDomainEvent;
