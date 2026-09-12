using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.SubmitMaintenanceQuote;

/// <summary>Quote fields live directly on MaintenanceRequest — no separate MaintenanceQuote entity, no revision/versioning subsystem (approved MVP design).</summary>
public sealed class SubmitMaintenanceQuoteCommandHandler : ICommandHandler<SubmitMaintenanceQuoteCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Un devis ne peut pas être soumis pour cette demande dans son état actuel.";
    private const string InvalidCostError = "Le coût estimé doit être strictement positif.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.QuotePending];

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public SubmitMaintenanceQuoteCommandHandler(IMaintenanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(SubmitMaintenanceQuoteCommand command, CancellationToken cancellationToken)
    {
        if (command.EstimatedCost <= 0)
        {
            return Result.Failure(InvalidCostError, ErrorType.Validation);
        }

        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, MaintenanceRequestStatus.QuoteSubmitted, requiredGarageUserId: command.GarageUserId,
            requiredOwnerUserId: null, estimatedCost: command.EstimatedCost, finalCost: null, reason: null, cancelledByUserId: null, utcNow,
            cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.OwnerUserId, NotificationCategory.Maintenance, "maintenance.quote-submitted",
                new Dictionary<string, string> { ["EstimatedCost"] = command.EstimatedCost.ToString("F2") }, IsMandatory: false,
                SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
