using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;

public sealed record CreateAssignmentCommand(
    Guid RequestingUserId,
    Guid DriverId,
    Guid VehicleId,
    DateOnly StartDate,
    DateOnly? EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DaysOfWeek DaysOfWeek) : ICommand<Result<Guid>>;
