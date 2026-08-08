namespace SmartTaxi.Application.Fleet.Assignments.Queries.ValidateAssignmentAvailability;

public sealed record AssignmentAvailabilityReport(bool IsAvailable, bool DriverConflict, bool VehicleConflict);
