using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.ForceCancelRoadsideRequest;

/// <summary>
/// Admin escape hatch — allowed from any non-terminal status (see
/// IRoadsideForceCancelRepository). The repository itself decides whether a
/// Fleet release is even attempted (only immobilizing service types currently
/// InProgress), so this handler never branches on service type itself.
/// </summary>
public sealed class ForceCancelRoadsideRequestCommandHandler : ICommandHandler<ForceCancelRoadsideRequestCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande est déjà dans un état terminal.";
    private const string ReasonRequiredError = "Un motif est requis pour une annulation forcée.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsideForceCancelRepository _forceCancelRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ForceCancelRoadsideRequestCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsideForceCancelRepository forceCancelRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _forceCancelRepository = forceCancelRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ForceCancelRoadsideRequestCommand command, CancellationToken cancellationToken)
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

        var succeeded = await _forceCancelRepository.TryForceCancelAsync(request.Id, command.AdminUserId, command.Reason, DateTime.UtcNow, cancellationToken);

        if (!succeeded)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.request.cancelled", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

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
