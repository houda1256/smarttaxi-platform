using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.SubmitVehicleForVerification;

/// <summary>
/// Registration already places a vehicle in PendingVerification, so this is
/// an explicit, idempotent confirmation step (e.g. "I've uploaded my
/// documents, please review") rather than a status transition of its own.
/// </summary>
public sealed class SubmitVehicleForVerificationCommandHandler : ICommandHandler<SubmitVehicleForVerificationCommand, Result>
{
    private const string NotFoundError = "Véhicule introuvable.";
    private const string NotPendingError = "Ce véhicule n'est pas en attente de vérification.";

    private readonly IVehicleRepository _repository;

    public SubmitVehicleForVerificationCommandHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitVehicleForVerificationCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OperationalStatus != VehicleOperationalStatus.PendingVerification)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
