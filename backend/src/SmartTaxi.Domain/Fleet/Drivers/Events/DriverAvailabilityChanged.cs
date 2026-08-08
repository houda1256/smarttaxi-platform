using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Domain.Fleet.Drivers.Events;

public sealed record DriverAvailabilityChanged(Guid DriverProfileId, DriverAvailabilityStatus NewStatus, DateTime OccurredAtUtc)
    : IDomainEvent;
