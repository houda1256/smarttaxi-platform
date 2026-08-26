using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceRequest;

public sealed record RespondToMaintenanceRequestCommand(Guid RequestId, Guid GarageUserId, bool IsAccepted, string? RejectionReason) : ICommand<Result>;
