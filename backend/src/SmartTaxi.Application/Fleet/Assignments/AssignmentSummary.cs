using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments;

public sealed record AssignmentSummary(
    Guid Id, Guid DriverId, Guid VehicleId, Guid OwnerId, DateOnly StartDate, DateOnly? EndDate,
    TimeOnly? StartTime, TimeOnly? EndTime, DaysOfWeek DaysOfWeek, AssignmentStatus Status, Guid AssignedBy,
    DateTime CreatedAt, DateTime UpdatedAt)
{
    public static AssignmentSummary FromEntity(DriverVehicleAssignment assignment) => new(
        assignment.Id, assignment.DriverId, assignment.VehicleId, assignment.OwnerId, assignment.StartDate,
        assignment.EndDate, assignment.StartTime, assignment.EndTime, assignment.DaysOfWeek, assignment.Status,
        assignment.AssignedBy, assignment.CreatedAt, assignment.UpdatedAt);
}
