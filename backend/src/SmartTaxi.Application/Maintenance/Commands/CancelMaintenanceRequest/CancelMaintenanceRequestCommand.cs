using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.CancelMaintenanceRequest;

public sealed record CancelMaintenanceRequestCommand(Guid RequestId, Guid OwnerUserId, string? Reason) : ICommand<Result>;
