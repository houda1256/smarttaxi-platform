using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceRequest;

/// <summary>
/// Garage refuses the request BEFORE any quote exists (Rejected) — distinct
/// from QuoteRejected (owner refuses a submitted quote) and Cancelled
/// (owner/admin withdraws), per the approved status semantics. The atomic
/// TryTransitionAsync guard bakes GarageUserId==caller into the same
/// statement as the status guard — garage A can never respond to garage B's
/// request, and the guard is defensive even though only one garage is ever
/// attached to a request by design (no broadcast/race).
/// </summary>
public sealed class RespondToMaintenanceRequestCommandHandler : ICommandHandler<RespondToMaintenanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande n'est plus en attente de réponse du garage.";
    private const string ReasonRequiredError = "Un motif de refus est requis.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.PendingGarageResponse];

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RespondToMaintenanceRequestCommandHandler(IMaintenanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(RespondToMaintenanceRequestCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsAccepted && string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return Result.Failure(ReasonRequiredError, ErrorType.Validation);
        }

        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        // Confirmed -> QuotePending is an immediate, actor-less system transition per the spec's own lifecycle
        // (no separate action happens in between) — collapsed into this one atomic step, same convention as
        // Requested -> PendingGarageResponse collapsing at creation. ConfirmedAtUtc still records the moment.
        var newStatus = command.IsAccepted ? MaintenanceRequestStatus.QuotePending : MaintenanceRequestStatus.Rejected;
        var utcNow = DateTime.UtcNow;

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, newStatus, requiredGarageUserId: command.GarageUserId, requiredOwnerUserId: null,
            estimatedCost: null, finalCost: null, reason: command.IsAccepted ? null : command.RejectionReason, cancelledByUserId: null, utcNow,
            cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.OwnerUserId, NotificationCategory.Maintenance,
                command.IsAccepted ? "maintenance.request-confirmed" : "maintenance.request-rejected", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
