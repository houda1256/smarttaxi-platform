using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.ForceCancelMaintenanceRequest;

/// <summary>
/// Admin escape hatch — allowed from any non-terminal status (see
/// IMaintenanceForceCancelRepository). Always attempts a best-effort Fleet
/// release unconditionally; it is a safe no-op when the vehicle was never
/// UnderMaintenance or already changed for another reason, so this handler
/// never needs to branch on the request's current status itself.
/// </summary>
public sealed class ForceCancelMaintenanceRequestCommandHandler : ICommandHandler<ForceCancelMaintenanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande est déjà dans un état terminal.";
    private const string ReasonRequiredError = "Un motif est requis pour une annulation forcée.";

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IMaintenanceForceCancelRepository _forceCancelRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ForceCancelMaintenanceRequestCommandHandler(
        IMaintenanceRequestRepository requestRepository, IMaintenanceForceCancelRepository forceCancelRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _forceCancelRepository = forceCancelRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ForceCancelMaintenanceRequestCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(ReasonRequiredError, ErrorType.Validation);
        }

        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var succeeded = await _forceCancelRepository.TryForceCancelAsync(
            request.Id, command.AdminUserId, request.VehicleId, command.Reason, DateTime.UtcNow, cancellationToken);

        if (!succeeded)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.OwnerUserId, NotificationCategory.Maintenance, "maintenance.cancelled", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.GarageUserId, NotificationCategory.Maintenance, "maintenance.cancelled", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
