using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.AcceptRoadsideJob;

/// <summary>
/// The atomic TryRespondAsync guard bakes PartnerUserId==caller into the same
/// statement as the Status==PendingPartnerResponse guard — a partner can never
/// accept a job they were not the SelectedPartnerUserId for, and two
/// concurrent accept attempts (only one of which can even legally originate
/// from the true selected partner) resolve to exactly one success.
/// </summary>
public sealed class AcceptRoadsideJobCommandHandler : ICommandHandler<AcceptRoadsideJobCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande n'est plus en attente de votre réponse.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public AcceptRoadsideJobCommandHandler(IRoadsideAssistanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(AcceptRoadsideJobCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var accepted = await _requestRepository.TryRespondAsync(
            request.Id, command.PartnerUserId, RoadsidePartnerResponse.Accepted, command.EstimatedCost, rejectionReason: null, DateTime.UtcNow,
            cancellationToken);

        if (!accepted)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.partner.accepted", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
