using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.CreateMaintenanceRequest;

/// <summary>
/// Owner-only, one manually selected garage (approved MVP design — no
/// broadcast/race). VehicleId ownership is re-checked server-side against
/// Fleet's own IVehicleRepository — never trusted from the request body, even
/// though the caller's identity itself comes from the JWT at the API layer.
/// </summary>
public sealed class CreateMaintenanceRequestCommandHandler : ICommandHandler<CreateMaintenanceRequestCommand, Result<Guid>>
{
    private const string VehicleNotFoundError = "Véhicule introuvable.";
    private const string NotOwnerError = "Vous ne pouvez demander une maintenance que pour vos propres véhicules.";
    private const string GarageNotFoundError = "Garage introuvable ou inactif.";
    private const string ActiveRequestExistsError = "Ce véhicule a déjà une demande de maintenance active.";

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IGarageProfileRepository _garageProfileRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CreateMaintenanceRequestCommandHandler(
        IMaintenanceRequestRepository requestRepository, IVehicleRepository vehicleRepository, IGarageProfileRepository garageProfileRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
        _garageProfileRepository = garageProfileRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<Guid>> Handle(CreateMaintenanceRequestCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<Guid>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OwnerId != command.OwnerUserId)
        {
            return Result<Guid>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var garageProfile = await _garageProfileRepository.GetByUserIdAsync(command.GarageUserId, cancellationToken);

        if (garageProfile is null || !garageProfile.IsActive)
        {
            return Result<Guid>.Failure(GarageNotFoundError, ErrorType.NotFound);
        }

        if (await _requestRepository.HasActiveRequestForVehicleAsync(command.VehicleId, cancellationToken))
        {
            return Result<Guid>.Failure(ActiveRequestExistsError, ErrorType.Conflict);
        }

        MaintenanceRequest request;

        try
        {
            request = MaintenanceRequest.Create(command.VehicleId, command.OwnerUserId, command.GarageUserId, command.Description, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        if (!await _requestRepository.TryAddAsync(request, cancellationToken))
        {
            return Result<Guid>.Failure(ActiveRequestExistsError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                command.GarageUserId, NotificationCategory.Maintenance, "maintenance.request-created",
                new Dictionary<string, string> { ["Description"] = request.Description }, IsMandatory: false, SourceType: "MaintenanceRequest",
                SourceId: request.Id),
            cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}
