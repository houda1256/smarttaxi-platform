using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.DriverEnRoute;

public sealed class DriverEnRouteCommandHandler : ICommandHandler<DriverEnRouteCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotAcceptedError = "Cette course n'a pas encore été acceptée par le chauffeur.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public DriverEnRouteCommandHandler(IRideRepository rideRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(DriverEnRouteCommand command, CancellationToken cancellationToken)
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
            ride.Id, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, command.RequestingUserId, null,
            DateTime.UtcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotAcceptedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
