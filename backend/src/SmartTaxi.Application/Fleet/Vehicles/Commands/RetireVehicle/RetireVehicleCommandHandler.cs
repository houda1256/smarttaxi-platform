using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.RetireVehicle;

/// <summary>Permanent, terminal — the row and all its history/documents are preserved, never deleted.</summary>
public sealed class RetireVehicleCommandHandler : ICommandHandler<RetireVehicleCommand, Result>
{
    private const string NotFoundError = "Véhicule introuvable.";
    private const string AlreadyRetiredError = "Ce véhicule est déjà retiré.";

    private readonly IVehicleRepository _repository;

    public RetireVehicleCommandHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RetireVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OperationalStatus == VehicleOperationalStatus.Retired)
        {
            return Result.Failure(AlreadyRetiredError, ErrorType.Conflict);
        }

        var retired = await _repository.TryRetireAsync(vehicle.Id, DateTime.UtcNow, cancellationToken);

        if (!retired)
        {
            return Result.Failure(AlreadyRetiredError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
