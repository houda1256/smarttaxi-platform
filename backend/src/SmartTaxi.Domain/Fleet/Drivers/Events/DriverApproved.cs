using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Drivers.Events;

public sealed record DriverApproved(Guid DriverProfileId, Guid ApprovedBy, DateTime OccurredAtUtc) : IDomainEvent;
