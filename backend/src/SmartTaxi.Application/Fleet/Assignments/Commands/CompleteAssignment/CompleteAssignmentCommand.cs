using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.CompleteAssignment;

/// <summary>
/// MileageStart/MileageEnd are the odometer readings for this usage period, entered
/// by the fleet manager/owner at completion time (no live telemetry integration yet).
/// RideCount/RevenueGenerated/IncidentCount default to zero/null until the Rides
/// module can supply real figures for the period.
/// </summary>
public sealed record CompleteAssignmentCommand(
    Guid RequestingUserId,
    Guid AssignmentId,
    int MileageStart,
    int MileageEnd,
    int RideCount = 0,
    decimal? RevenueGenerated = null,
    int IncidentCount = 0) : ICommand<Result>;
