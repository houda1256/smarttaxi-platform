using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.CancelMaintenanceRequest;

/// <summary>
/// Owner-only, and only legal before the vehicle has been physically
/// received by the garage — confirmed from the approved state machine, the
/// vehicle never entered UnderMaintenance in any of these source statuses, so
/// this never touches Fleet (unlike ForceCancel, which may need to release a
/// vehicle from InProgress/WaitingForParts).
/// </summary>
public sealed class CancelMaintenanceRequestCommandHandler : ICommandHandler<CancelMaintenanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande ne peut plus être annulée à ce stade.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses =
    [
        MaintenanceRequestStatus.PendingGarageResponse, MaintenanceRequestStatus.QuotePending, MaintenanceRequestStatus.QuoteSubmitted
    ];

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CancelMaintenanceRequestCommandHandler(IMaintenanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(CancelMaintenanceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, MaintenanceRequestStatus.Cancelled, requiredGarageUserId: null,
            requiredOwnerUserId: command.OwnerUserId, estimatedCost: null, finalCost: null, reason: command.Reason,
            cancelledByUserId: command.OwnerUserId, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.GarageUserId, NotificationCategory.Maintenance, "maintenance.cancelled", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
