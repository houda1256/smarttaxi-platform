using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.EscalateRoadsideRequestToMaintenance;

/// <summary>
/// Only the vehicle's actual owner may escalate — a direct consequence of
/// Maintenance's own MaintenanceRequest.OwnerUserId invariant (approved plan,
/// Q4): if a Driver created the original Roadside request, the Driver may NOT
/// trigger escalation, since the resulting MaintenanceRequest must be owned
/// by the real vehicle owner. GarageUserId is always manually supplied by the
/// owner — never auto-selected.
/// </summary>
public sealed class EscalateRoadsideRequestToMaintenanceCommandHandler
    : ICommandHandler<EscalateRoadsideRequestToMaintenanceCommand, Result<Guid>>
{
    private const string VehicleNotFoundError = "Véhicule introuvable.";
    private const string NotOwnerError = "Seul le propriétaire du véhicule peut transférer cette demande vers la maintenance.";
    private const string RequestNotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande d'assistance routière n'est pas complétée.";
    private const string MaintenanceConflictError = "Ce véhicule a déjà une demande de maintenance active.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsideEscalationRepository _escalationRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public EscalateRoadsideRequestToMaintenanceCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsideEscalationRepository escalationRepository,
        IVehicleRepository vehicleRepository)
    {
        _requestRepository = requestRepository;
        _escalationRepository = escalationRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<Guid>> Handle(EscalateRoadsideRequestToMaintenanceCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<Guid>.Failure(RequestNotFoundError, ErrorType.NotFound);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<Guid>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OwnerId != command.CallerUserId)
        {
            return Result<Guid>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var description = string.IsNullOrWhiteSpace(command.Description)
            ? $"Transféré depuis la demande d'assistance routière {request.Id}."
            : command.Description;

        var result = await _escalationRepository.TryEscalateAsync(
            request.Id, command.CallerUserId, command.GarageUserId, description, DateTime.UtcNow, cancellationToken);

        return result.Outcome switch
        {
            RoadsideEscalationOutcome.Escalated or RoadsideEscalationOutcome.AlreadyEscalated => Result<Guid>.Success(result.MaintenanceRequestId!.Value),
            RoadsideEscalationOutcome.MaintenanceConflict => Result<Guid>.Failure(MaintenanceConflictError, ErrorType.Conflict),
            _ => Result<Guid>.Failure(NotEligibleError, ErrorType.Conflict)
        };
    }
}
