using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.ApproveVehicle;

/// <summary>
/// Platform-level verification — an owner can never approve their own
/// vehicle's verification, regardless of any other permission they hold.
/// </summary>
public sealed class ApproveVehicleCommandHandler : ICommandHandler<ApproveVehicleCommand, Result>
{
    private const string NotFoundError = "Véhicule introuvable.";
    private const string NotPendingError = "Ce véhicule n'est plus en attente de vérification.";
    private const string SelfApprovalError = "Un propriétaire ne peut pas approuver son propre véhicule.";

    private readonly IVehicleRepository _repository;

    public ApproveVehicleCommandHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ApproveVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OwnerId == command.ReviewerId)
        {
            return Result.Failure(SelfApprovalError, ErrorType.Forbidden);
        }

        if (vehicle.OperationalStatus != VehicleOperationalStatus.PendingVerification)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var approved = await _repository.TryApproveAsync(vehicle.Id, command.ReviewerId, DateTime.UtcNow, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
