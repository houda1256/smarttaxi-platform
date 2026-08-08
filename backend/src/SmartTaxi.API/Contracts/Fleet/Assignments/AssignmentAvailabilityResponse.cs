using SmartTaxi.Application.Fleet.Assignments.Queries.ValidateAssignmentAvailability;

namespace SmartTaxi.API.Contracts.Fleet.Assignments;

public sealed record AssignmentAvailabilityResponse(bool IsAvailable, bool DriverConflict, bool VehicleConflict)
{
    public static AssignmentAvailabilityResponse FromReport(AssignmentAvailabilityReport report) => new(
        report.IsAvailable, report.DriverConflict, report.VehicleConflict);
}
