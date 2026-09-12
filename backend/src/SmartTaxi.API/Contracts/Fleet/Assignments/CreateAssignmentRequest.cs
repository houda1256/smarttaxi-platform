namespace SmartTaxi.API.Contracts.Fleet.Assignments;

public sealed record CreateAssignmentRequest(
    Guid DriverId, Guid VehicleId, DateOnly StartDate, DateOnly? EndDate, TimeOnly? StartTime, TimeOnly? EndTime,
    IReadOnlyCollection<string> DaysOfWeek);
