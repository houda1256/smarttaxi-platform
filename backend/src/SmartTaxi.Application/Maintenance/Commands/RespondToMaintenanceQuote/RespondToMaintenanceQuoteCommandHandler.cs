using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceQuote;

/// <summary>QuoteRejected is distinct from Rejected/Cancelled — the owner refusing a submitted quote, a different actor/meaning from the garage's own pre-quote refusal or a withdrawal.</summary>
public sealed class RespondToMaintenanceQuoteCommandHandler : ICommandHandler<RespondToMaintenanceQuoteCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Aucun devis en attente de réponse pour cette demande.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.QuoteSubmitted];

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RespondToMaintenanceQuoteCommandHandler(IMaintenanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(RespondToMaintenanceQuoteCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var newStatus = command.IsAccepted ? MaintenanceRequestStatus.QuoteAccepted : MaintenanceRequestStatus.QuoteRejected;
        var utcNow = DateTime.UtcNow;

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, newStatus, requiredGarageUserId: null, requiredOwnerUserId: command.OwnerUserId,
            estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.GarageUserId, NotificationCategory.Maintenance,
                command.IsAccepted ? "maintenance.quote-accepted" : "maintenance.quote-rejected", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
