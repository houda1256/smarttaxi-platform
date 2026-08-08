using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Drivers.Events;

public sealed record DriverRejected(Guid DriverProfileId, Guid RejectedBy, string Reason, DateTime OccurredAtUtc) : IDomainEvent;
