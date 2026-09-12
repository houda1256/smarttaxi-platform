using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Maintenance.Events;

/// <summary>Genuinely raised (unit-testable), same convention as every other module's Created event — no dispatcher/outbox consumes these; INotificationDispatcher is the real integration point Maintenance's Application handlers call directly.</summary>
public sealed record GarageProfileRegistered(Guid GarageProfileId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record MaintenanceRequestCreated(Guid MaintenanceRequestId, Guid OwnerUserId, Guid GarageUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record MaintenanceRecordCreated(Guid MaintenanceRecordId, Guid MaintenanceRequestId, Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;
