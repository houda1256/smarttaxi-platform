using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Policies;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.StartRoadsideIntervention;

/// <summary>
/// Routes to the atomic Fleet-coordination repository ONLY when
/// RoadsideServiceTypePolicy.RequiresVehicleImmobilization is true (approved
/// plan, Q3) — the single centralized policy decision, never duplicated as a
/// per-handler service-type list. Non-immobilizing service types transition
/// via the plain repository call and never touch Fleet at all.
/// </summary>
public sealed class StartRoadsideInterventionCommandHandler : ICommandHandler<StartRoadsideInterventionCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string RequestNotEligibleError = "Cette demande n'est pas prête à démarrer (le partenaire doit être arrivé).";
    private const string VehicleNotEligibleError = "Le véhicule n'est pas dans un état permettant une intervention.";

    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.PartnerArrived];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsideWorkStartRepository _workStartRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public StartRoadsideInterventionCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsideWorkStartRepository workStartRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _workStartRepository = workStartRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(StartRoadsideInterventionCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        if (RoadsideServiceTypePolicy.RequiresVehicleImmobilization(request.ServiceType))
        {
            var outcome = await _workStartRepository.TryStartAsync(request.Id, command.PartnerUserId, request.VehicleId, utcNow, cancellationToken);

            if (outcome != RoadsideWorkStartResult.Started)
            {
                var error = outcome == RoadsideWorkStartResult.VehicleNotEligible ? VehicleNotEligibleError : RequestNotEligibleError;
                return Result.Failure(error, ErrorType.Conflict);
            }
        }
        else
        {
            var transitioned = await _requestRepository.TryTransitionAsync(
                request.Id, AllowedFromStatuses, RoadsideRequestStatus.InProgress, requiredRequesterUserId: null,
                requiredPartnerUserId: command.PartnerUserId, finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false,
                utcNow, cancellationToken);

            if (!transitioned)
            {
                return Result.Failure(RequestNotEligibleError, ErrorType.Conflict);
            }
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.intervention.started", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
