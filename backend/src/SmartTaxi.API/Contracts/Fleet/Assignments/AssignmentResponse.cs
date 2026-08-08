using SmartTaxi.Application.Fleet.Assignments;
using DaysOfWeekFlags = SmartTaxi.Domain.Fleet.Assignments.Enums.DaysOfWeek;

namespace SmartTaxi.API.Contracts.Fleet.Assignments;

public sealed record AssignmentResponse(
    Guid Id, Guid DriverId, Guid VehicleId, Guid OwnerId, DateOnly StartDate, DateOnly? EndDate,
    TimeOnly? StartTime, TimeOnly? EndTime, IReadOnlyCollection<string> DaysOfWeek, string Status, Guid AssignedBy,
    DateTime CreatedAt, DateTime UpdatedAt)
{
    private static readonly DaysOfWeekFlags[] SingleDays =
    [
        DaysOfWeekFlags.Monday, DaysOfWeekFlags.Tuesday, DaysOfWeekFlags.Wednesday, DaysOfWeekFlags.Thursday,
        DaysOfWeekFlags.Friday, DaysOfWeekFlags.Saturday, DaysOfWeekFlags.Sunday
    ];

    public static AssignmentResponse FromSummary(AssignmentSummary summary) => new(
        summary.Id, summary.DriverId, summary.VehicleId, summary.OwnerId, summary.StartDate, summary.EndDate,
        summary.StartTime, summary.EndTime,
        SingleDays.Where(day => summary.DaysOfWeek.HasFlag(day)).Select(day => day.ToString()).ToList(),
        summary.Status.ToString(), summary.AssignedBy, summary.CreatedAt, summary.UpdatedAt);
}
