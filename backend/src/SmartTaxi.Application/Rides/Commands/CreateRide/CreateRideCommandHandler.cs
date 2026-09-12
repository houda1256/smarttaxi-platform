using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Commands.CreateRide;

public sealed class CreateRideCommandHandler : ICommandHandler<CreateRideCommand, Result<Guid>>
{
    private const string DefaultCurrency = "TND";

    private readonly IRideRepository _repository;

    public CreateRideCommandHandler(IRideRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateRideCommand command, CancellationToken cancellationToken)
    {
        if (!GeoCoordinate.TryCreate(command.PickupLatitude, command.PickupLongitude, out var pickup, out var pickupError))
        {
            return Result<Guid>.Failure(pickupError, ErrorType.Validation);
        }

        if (!GeoCoordinate.TryCreate(command.DestinationLatitude, command.DestinationLongitude, out var destination, out var destinationError))
        {
            return Result<Guid>.Failure(destinationError, ErrorType.Validation);
        }

        Ride ride;

        try
        {
            ride = Ride.Create(
                command.CustomerId, command.RideType, command.PickupAddress, pickup, command.DestinationAddress,
                destination, command.ScheduledAt, command.PassengerCount, command.LuggageCount,
                command.NeedsAirConditioning, command.NeedsAccessibleVehicle, command.HasChildSeatRequest,
                command.HasPet, command.PreferredVehicleCategory, command.PreferredPaymentMethod,
                command.SpecialInstructions, DefaultCurrency, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.AddAsync(ride, cancellationToken);

        return Result<Guid>.Success(ride.Id);
    }
}
