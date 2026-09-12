using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.RejectRoadsideJob;

/// <summary>Rejected is not terminal — the requester may explicitly Reselect (ReselectRoadsideRequestCommand) to pick another partner.</summary>
public sealed class RejectRoadsideJobCommandHandler : ICommandHandler<RejectRoadsideJobCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande n'est plus en attente de votre réponse.";
    private const string ReasonRequiredError = "Un motif de refus est requis.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RejectRoadsideJobCommandHandler(IRoadsideAssistanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(RejectRoadsideJobCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return Result.Failure(ReasonRequiredError, ErrorType.Validation);
        }

        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var rejected = await _requestRepository.TryRespondAsync(
            request.Id, command.PartnerUserId, RoadsidePartnerResponse.Rejected, estimatedCost: null, command.RejectionReason, DateTime.UtcNow,
            cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.partner.rejected", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
