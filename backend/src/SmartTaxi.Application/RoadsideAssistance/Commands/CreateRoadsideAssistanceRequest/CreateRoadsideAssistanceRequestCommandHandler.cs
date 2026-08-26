using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.CreateRoadsideAssistanceRequest;

/// <summary>
/// VehicleId ownership/authorization is re-checked server-side, never trusted
/// from the request body: a TaxiOwner requester must own the vehicle
/// directly; a Driver requester must hold an Active DriverVehicleAssignment
/// for it (approved plan, §Requester Authorization). RideId, when supplied,
/// is validated read-only against Rides — never mutated (Rides integration
/// remains read-only per the approved plan).
/// </summary>
public sealed class CreateRoadsideAssistanceRequestCommandHandler : ICommandHandler<CreateRoadsideAssistanceRequestCommand, Result<Guid>>
{
    private const string VehicleNotFoundError = "Véhicule introuvable.";
    private const string NotOwnerError = "Vous ne pouvez demander une assistance que pour vos propres véhicules.";
    private const string NoActiveAssignmentError = "Vous n'avez pas d'affectation active pour ce véhicule.";
    private const string RideNotFoundError = "Course introuvable.";
    private const string RideNotAssociatedError = "Vous n'êtes pas associé à cette course.";
    private const string RideVehicleMismatchError = "Le véhicule ne correspond pas à cette course.";
    private const string ActiveRequestExistsError = "Ce véhicule a déjà une demande d'assistance routière active.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverVehicleAssignmentRepository _assignmentRepository;
    private readonly IRideRepository _rideRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CreateRoadsideAssistanceRequestCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IVehicleRepository vehicleRepository,
        IDriverVehicleAssignmentRepository assignmentRepository, IRideRepository rideRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
        _assignmentRepository = assignmentRepository;
        _rideRepository = rideRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<Guid>> Handle(CreateRoadsideAssistanceRequestCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<Guid>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        if (command.RequesterRole == RoadsideRequesterRole.TaxiOwner)
        {
            if (vehicle.OwnerId != command.RequesterUserId)
            {
                return Result<Guid>.Failure(NotOwnerError, ErrorType.Forbidden);
            }
        }
        else
        {
            var assignments = await _assignmentRepository.GetActiveOrPendingForDriverAsync(command.RequesterUserId, null, cancellationToken);

            if (!assignments.Any(assignment => assignment.VehicleId == command.VehicleId && assignment.Status == AssignmentStatus.Active))
            {
                return Result<Guid>.Failure(NoActiveAssignmentError, ErrorType.Forbidden);
            }
        }

        if (command.RideId is { } rideId)
        {
            var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken);

            if (ride is null)
            {
                return Result<Guid>.Failure(RideNotFoundError, ErrorType.NotFound);
            }

            if (ride.CustomerId != command.RequesterUserId && ride.SelectedDriverId != command.RequesterUserId)
            {
                return Result<Guid>.Failure(RideNotAssociatedError, ErrorType.Forbidden);
            }

            if (ride.VehicleId is not null && ride.VehicleId != command.VehicleId)
            {
                return Result<Guid>.Failure(RideVehicleMismatchError, ErrorType.Validation);
            }
        }

        if (await _requestRepository.HasActiveRequestForVehicleAsync(command.VehicleId, cancellationToken))
        {
            return Result<Guid>.Failure(ActiveRequestExistsError, ErrorType.Conflict);
        }

        RoadsideAssistanceRequest request;

        try
        {
            request = RoadsideAssistanceRequest.Create(
                command.RequesterUserId, command.RequesterRole, command.VehicleId, command.RideId, command.ServiceType, command.Urgency,
                command.Description, command.Latitude, command.Longitude, command.Address, command.City, DateTime.UtcNow);
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
                request.RequesterUserId, NotificationCategory.Roadside, "roadside.request.created", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}
