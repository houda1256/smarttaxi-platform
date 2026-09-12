using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.StartRide;

/// <summary>Only reachable from PassengerOnBoard — a Driver can never start before arrival and passenger confirmation.</summary>
public sealed class StartRideCommandHandler : ICommandHandler<StartRideCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotReadyError = "Le passager n'est pas encore confirmé à bord.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public StartRideCommandHandler(IRideRepository rideRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(StartRideCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.SelectedDriverId is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

        if (driver is null || driver.UserId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.PassengerOnBoard, RideStatus.InProgress, command.RequestingUserId, null,
            DateTime.UtcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotReadyError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
