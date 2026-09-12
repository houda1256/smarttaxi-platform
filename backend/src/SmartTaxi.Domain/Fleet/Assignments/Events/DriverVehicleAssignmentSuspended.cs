using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Assignments.Events;

public sealed record DriverVehicleAssignmentSuspended(Guid AssignmentId, DateTime OccurredAtUtc) : IDomainEvent;
