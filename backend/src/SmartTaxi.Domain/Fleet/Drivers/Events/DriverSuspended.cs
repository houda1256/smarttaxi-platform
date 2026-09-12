using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Drivers.Events;

public sealed record DriverSuspended(Guid DriverProfileId, DateTime OccurredAtUtc) : IDomainEvent;
