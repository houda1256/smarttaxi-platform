using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.RejectVehicle;

public sealed class RejectVehicleCommandHandler : ICommandHandler<RejectVehicleCommand, Result>
{
    private const string NotFoundError = "Véhicule introuvable.";
    private const string NotPendingError = "Ce véhicule n'est plus en attente de vérification.";
    private const string SelfApprovalError = "Un propriétaire ne peut pas examiner son propre véhicule.";
    private const string MissingReasonError = "Un motif de rejet est requis.";

    private readonly IVehicleRepository _repository;

    public RejectVehicleCommandHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RejectVehicleCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(MissingReasonError, ErrorType.Validation);
        }

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

        var rejected = await _repository.TryRejectAsync(vehicle.Id, command.ReviewerId, DateTime.UtcNow, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
