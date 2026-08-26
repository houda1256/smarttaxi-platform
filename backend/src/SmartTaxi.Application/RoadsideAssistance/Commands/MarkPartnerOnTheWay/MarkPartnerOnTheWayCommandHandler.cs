using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerOnTheWay;

public sealed class MarkPartnerOnTheWayCommandHandler : ICommandHandler<MarkPartnerOnTheWayCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas dans un état permettant ce changement.";

    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.Accepted];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public MarkPartnerOnTheWayCommandHandler(IRoadsideAssistanceRequestRepository requestRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(MarkPartnerOnTheWayCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, RoadsideRequestStatus.PartnerOnTheWay, requiredRequesterUserId: null,
            requiredPartnerUserId: command.PartnerUserId, finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false,
            DateTime.UtcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.partner.enroute", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
