using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Queries.ValidateAssignmentAvailability;

public sealed record ValidateAssignmentAvailabilityQuery(
    Guid DriverId, Guid VehicleId, DateOnly StartDate, DateOnly? EndDate, DaysOfWeek DaysOfWeek)
    : IQuery<AssignmentAvailabilityReport>;
