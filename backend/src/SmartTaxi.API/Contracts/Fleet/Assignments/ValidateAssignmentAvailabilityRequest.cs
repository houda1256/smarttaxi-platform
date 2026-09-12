namespace SmartTaxi.API.Contracts.Fleet.Assignments;

public sealed record ValidateAssignmentAvailabilityRequest(
    Guid DriverId, Guid VehicleId, DateOnly StartDate, DateOnly? EndDate, IReadOnlyCollection<string> DaysOfWeek);
