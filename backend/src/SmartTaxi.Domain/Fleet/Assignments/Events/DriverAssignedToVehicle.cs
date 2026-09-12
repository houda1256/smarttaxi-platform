using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Assignments.Events;

public sealed record DriverAssignedToVehicle(Guid AssignmentId, Guid DriverId, Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;
