using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Assignments.Events;

public sealed record DriverUnassignedFromVehicle(Guid AssignmentId, Guid DriverId, Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;
