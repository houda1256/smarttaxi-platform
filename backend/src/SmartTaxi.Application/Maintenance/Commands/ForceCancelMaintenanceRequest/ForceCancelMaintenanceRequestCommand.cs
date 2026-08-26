using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.ForceCancelMaintenanceRequest;

public sealed record ForceCancelMaintenanceRequestCommand(Guid RequestId, Guid AdminUserId, string Reason) : ICommand<Result>;
