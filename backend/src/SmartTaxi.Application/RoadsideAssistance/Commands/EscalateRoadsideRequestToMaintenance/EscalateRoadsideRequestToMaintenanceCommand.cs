using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.EscalateRoadsideRequestToMaintenance;

public sealed record EscalateRoadsideRequestToMaintenanceCommand(Guid RequestId, Guid CallerUserId, Guid GarageUserId, string? Description)
    : ICommand<Result<Guid>>;
