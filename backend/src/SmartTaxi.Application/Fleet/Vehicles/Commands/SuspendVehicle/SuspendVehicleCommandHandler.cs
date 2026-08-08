using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.SuspendVehicle;

/// <summary>
/// Day-to-day operational suspension is owner-controlled, unlike platform
/// verification (Approve/Reject), which an owner can never perform on their
/// own vehicle.
/// </summary>
public sealed class SuspendVehicleCommandHandler : ICommandHandler<SuspendVehicleCommand, Result>
{
    private const string NotFoundError = "Véhicule introuvable.";
    private const string NotActiveError = "Seul un véhicule actif peut être suspendu.";

    private readonly IVehicleRepository _repository;

    public SuspendVehicleCommandHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OperationalStatus != VehicleOperationalStatus.Active)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        var suspended = await _repository.TrySuspendAsync(vehicle.Id, DateTime.UtcNow, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
