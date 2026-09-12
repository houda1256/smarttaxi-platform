using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.CancelRoadsideAssistanceRequest;

/// <summary>
/// Requester-only, and only legal before the intervention has actually
/// started — confirmed from the approved state machine, the vehicle never
/// entered UnderRoadsideAssistance in any of these source statuses, so this
/// never touches Fleet (unlike ForceCancel, which may need to release a
/// vehicle from InProgress).
/// </summary>
public sealed class CancelRoadsideAssistanceRequestCommandHandler : ICommandHandler<CancelRoadsideAssistanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande ne peut plus être annulée à ce stade.";

    private static readonly RoadsideRequestStatus[] AllowedFromStatuses =
    [
        RoadsideRequestStatus.PartnersAvailable, RoadsideRequestStatus.PendingPartnerResponse, RoadsideRequestStatus.Accepted,
        RoadsideRequestStatus.PartnerOnTheWay, RoadsideRequestStatus.PartnerArrived
    ];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CancelRoadsideAssistanceRequestCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(CancelRoadsideAssistanceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, RoadsideRequestStatus.Cancelled, requiredRequesterUserId: command.RequesterUserId,
            requiredPartnerUserId: null, finalCost: null, command.Reason, cancelledByUserId: command.RequesterUserId,
            clearSelectedPartner: false, DateTime.UtcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        if (request.SelectedPartnerUserId is { } partnerUserId)
        {
            await _notificationDispatcher.DispatchAsync(
                new NotificationRequest(
                    partnerUserId, NotificationCategory.Roadside, "roadside.request.cancelled", new Dictionary<string, string>(),
                    IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
                cancellationToken);
        }

        return Result.Success();
    }
}
