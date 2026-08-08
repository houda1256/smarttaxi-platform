using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Drivers.Events;

public sealed record DriverProfileCreated(Guid DriverProfileId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;
