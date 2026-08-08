using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.UpdateVehicle;

public sealed class UpdateVehicleCommandHandler : ICommandHandler<UpdateVehicleCommand, Result>
{
    private const string NotFoundError = "Véhicule introuvable.";
    private const string DuplicatePlateError = "Une plaque d'immatriculation identique est déjà enregistrée.";
    private const string DuplicateVinError = "Un numéro VIN identique est déjà enregistré.";

    private readonly IVehicleRepository _repository;

    public UpdateVehicleCommandHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (await _repository.ExistsWithLicensePlateAsync(command.LicensePlate, command.VehicleId, cancellationToken))
        {
            return Result.Failure(DuplicatePlateError, ErrorType.Conflict);
        }

        if (!string.IsNullOrWhiteSpace(command.Vin)
            && await _repository.ExistsWithVinAsync(command.Vin, command.VehicleId, cancellationToken))
        {
            return Result.Failure(DuplicateVinError, ErrorType.Conflict);
        }

        vehicle.UpdateDetails(
            command.Brand, command.Model, command.Year, command.Color, command.LicensePlate, command.Vin,
            command.CurrentMileage, command.FuelType, command.TransmissionType, command.SeatCount,
            command.HasAirConditioning, command.IsAccessible, command.VehicleCategory, vehicle.MainPhotoReference,
            DateTime.UtcNow);

        await _repository.UpdateAsync(vehicle, cancellationToken);

        return Result.Success();
    }
}
