using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record RideSOSActivated(Guid SafetyEventId, Guid RideId, Guid TriggeredByUserId, DateTime OccurredAtUtc) : IDomainEvent;
