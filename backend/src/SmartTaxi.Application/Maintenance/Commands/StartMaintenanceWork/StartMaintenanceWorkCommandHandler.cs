using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.StartMaintenanceWork;

/// <summary>
/// Mandatory atomicity design (approved plan correction): delegates entirely
/// to IMaintenanceWorkStartRepository, whose single database transaction
/// guarantees MaintenanceRequest.Status == InProgress AND
/// Vehicle.OperationalStatus == UnderMaintenance together, or neither — this
/// handler never calls Fleet independently.
/// </summary>
public sealed class StartMaintenanceWorkCommandHandler : ICommandHandler<StartMaintenanceWorkCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string RequestNotEligibleError = "Cette demande n'est pas prête à démarrer (le véhicule doit avoir été réceptionné).";
    private const string VehicleNotEligibleError = "Le véhicule n'est pas dans un état permettant l'entrée en maintenance.";

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IMaintenanceWorkStartRepository _workStartRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public StartMaintenanceWorkCommandHandler(
        IMaintenanceRequestRepository requestRepository, IMaintenanceWorkStartRepository workStartRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _workStartRepository = workStartRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(StartMaintenanceWorkCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var outcome = await _workStartRepository.TryStartAsync(request.Id, command.GarageUserId, request.VehicleId, DateTime.UtcNow, cancellationToken);

        if (outcome != MaintenanceWorkStartResult.Started)
        {
            var error = outcome == MaintenanceWorkStartResult.VehicleNotEligible ? VehicleNotEligibleError : RequestNotEligibleError;
            return Result.Failure(error, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.OwnerUserId, NotificationCategory.Maintenance, "maintenance.work-started", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}
