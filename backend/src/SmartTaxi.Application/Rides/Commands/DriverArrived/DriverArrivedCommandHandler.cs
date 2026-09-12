using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.DriverArrived;

public sealed class DriverArrivedCommandHandler : ICommandHandler<DriverArrivedCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotEnRouteError = "Le chauffeur n'est pas en route pour cette course.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public DriverArrivedCommandHandler(IRideRepository rideRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(DriverArrivedCommand command, CancellationToken cancellationToken)
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

        var transitioned = await _rideRepository.TryMarkDriverArrivedAsync(ride.Id, DateTime.UtcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEnRouteError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
