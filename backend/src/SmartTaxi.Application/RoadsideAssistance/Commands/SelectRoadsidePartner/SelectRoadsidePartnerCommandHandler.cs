using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.SelectRoadsidePartner;

/// <summary>
/// Manual selection only — no broadcast/marketplace (approved plan, Q1). The
/// requester picks exactly one partner from the recommendation list; the
/// atomic TrySelectPartnerAsync guard bakes RequesterUserId==caller into the
/// same statement as the Status==PartnersAvailable guard, and inserts the new
/// selection-cycle history row in the same transaction. Also used to re-select
/// after an explicit Reselect (Rejected -&gt; PartnersAvailable).
/// </summary>
public sealed class SelectRoadsidePartnerCommandHandler : ICommandHandler<SelectRoadsidePartnerCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string PartnerNotFoundError = "Partenaire d'assistance routière introuvable ou inactif.";
    private const string NotEligibleError = "Cette demande n'est pas en attente de sélection de partenaire.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsidePartnerProfileRepository _partnerProfileRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public SelectRoadsidePartnerCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsidePartnerProfileRepository partnerProfileRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _partnerProfileRepository = partnerProfileRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(SelectRoadsidePartnerCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var partnerProfile = await _partnerProfileRepository.GetByUserIdAsync(command.PartnerUserId, cancellationToken);

        if (partnerProfile is null || !partnerProfile.IsActive)
        {
            return Result.Failure(PartnerNotFoundError, ErrorType.NotFound);
        }

        var selected = await _requestRepository.TrySelectPartnerAsync(
            request.Id, command.RequesterUserId, command.PartnerUserId, DateTime.UtcNow, cancellationToken);

        if (!selected)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                command.PartnerUserId, NotificationCategory.Roadside, "roadside.partner.selected", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
