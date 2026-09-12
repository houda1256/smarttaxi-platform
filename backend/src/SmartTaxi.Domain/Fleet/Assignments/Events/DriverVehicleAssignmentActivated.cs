using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Assignments.Events;

public sealed record DriverVehicleAssignmentActivated(Guid AssignmentId, DateTime OccurredAtUtc) : IDomainEvent;
