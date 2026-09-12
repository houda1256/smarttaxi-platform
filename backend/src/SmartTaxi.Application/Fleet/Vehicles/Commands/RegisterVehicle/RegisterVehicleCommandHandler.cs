using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.RegisterVehicle;

public sealed class RegisterVehicleCommandHandler : ICommandHandler<RegisterVehicleCommand, Result<Guid>>
{
    private const string DuplicatePlateError = "Une plaque d'immatriculation identique est déjà enregistrée.";
    private const string DuplicateVinError = "Un numéro VIN identique est déjà enregistré.";
    private const string FleetNotFoundError = "Flotte introuvable pour ce propriétaire.";

    private readonly IVehicleRepository _repository;
    private readonly IFleetRepository _fleetRepository;

    public RegisterVehicleCommandHandler(IVehicleRepository repository, IFleetRepository fleetRepository)
    {
        _repository = repository;
        _fleetRepository = fleetRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        if (command.FleetId is not null)
        {
            var fleet = await _fleetRepository.GetByIdAsync(command.FleetId.Value, cancellationToken);

            if (fleet is null || fleet.OwnerId != command.OwnerId)
            {
                return Result<Guid>.Failure(FleetNotFoundError, ErrorType.NotFound);
            }
        }

        if (await _repository.ExistsWithLicensePlateAsync(command.LicensePlate, null, cancellationToken))
        {
            return Result<Guid>.Failure(DuplicatePlateError, ErrorType.Conflict);
        }

        if (!string.IsNullOrWhiteSpace(command.Vin)
            && await _repository.ExistsWithVinAsync(command.Vin, null, cancellationToken))
        {
            return Result<Guid>.Failure(DuplicateVinError, ErrorType.Conflict);
        }

        var vehicle = Vehicle.Register(
            command.OwnerId, command.FleetId, command.Brand, command.Model, command.Year, command.Color,
            command.LicensePlate, command.Vin, command.CurrentMileage, command.FuelType, command.TransmissionType,
            command.SeatCount, command.HasAirConditioning, command.IsAccessible, command.VehicleCategory,
            mainPhotoReference: null, DateTime.UtcNow);

        await _repository.AddAsync(vehicle, cancellationToken);

        return Result<Guid>.Success(vehicle.Id);
    }
}
