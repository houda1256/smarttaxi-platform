using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Policies;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.CompleteRoadsideIntervention;

/// <summary>
/// Same immobilizing/non-immobilizing routing as StartRoadsideInterventionCommandHandler.
/// For immobilizing types, IRoadsideCompletionRepository's non-gating Fleet
/// release means an independently-Suspended/UnderMaintenance vehicle never
/// blocks completion and is never force-reactivated.
/// </summary>
public sealed class CompleteRoadsideInterventionCommandHandler : ICommandHandler<CompleteRoadsideInterventionCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas en cours d'intervention.";
    private const string InvalidCostError = "Le coût final doit être strictement positif.";

    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.InProgress];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsideCompletionRepository _completionRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CompleteRoadsideInterventionCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsideCompletionRepository completionRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _completionRepository = completionRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(CompleteRoadsideInterventionCommand command, CancellationToken cancellationToken)
    {
        if (command.FinalCost <= 0)
        {
            return Result.Failure(InvalidCostError, ErrorType.Validation);
        }

        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        if (RoadsideServiceTypePolicy.RequiresVehicleImmobilization(request.ServiceType))
        {
            var outcome = await _completionRepository.TryCompleteAsync(
                request.Id, command.PartnerUserId, request.VehicleId, command.FinalCost, utcNow, cancellationToken);

            if (outcome != RoadsideCompletionResult.Completed)
            {
                return Result.Failure(NotEligibleError, ErrorType.Conflict);
            }
        }
        else
        {
            var transitioned = await _requestRepository.TryTransitionAsync(
                request.Id, AllowedFromStatuses, RoadsideRequestStatus.Completed, requiredRequesterUserId: null,
                requiredPartnerUserId: command.PartnerUserId, command.FinalCost, reason: null, cancelledByUserId: null,
                clearSelectedPartner: false, utcNow, cancellationToken);

            if (!transitioned)
            {
                return Result.Failure(NotEligibleError, ErrorType.Conflict);
            }
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.intervention.completed",
                new Dictionary<string, string> { ["FinalCost"] = command.FinalCost.ToString("F2") }, IsMandatory: false,
                SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                command.PartnerUserId, NotificationCategory.Roadside, "roadside.intervention.completed", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
