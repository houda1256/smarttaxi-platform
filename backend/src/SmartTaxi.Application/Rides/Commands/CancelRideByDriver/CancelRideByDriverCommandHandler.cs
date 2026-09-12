using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CancelRideByDriver;

/// <summary>
/// Only reachable once the Driver has actually accepted (DriverAccepted
/// onward) — before that, the Driver's way out is DriverRejectRide, not this
/// command. No fee is charged to the Driver here; repeated Driver
/// cancellations are a Fleet-side quality signal (DriverProfile's own
/// CancellationCount), not something this command mutates directly.
/// </summary>
public sealed class CancelRideByDriverCommandHandler : ICommandHandler<CancelRideByDriverCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotCancellableByDriverError = "Cette course ne peut pas être annulée par le chauffeur à ce stade.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public CancelRideByDriverCommandHandler(IRideRepository rideRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(CancelRideByDriverCommand command, CancellationToken cancellationToken)
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

        if (ride.Status is not (RideStatus.DriverAccepted or RideStatus.DriverEnRoute or RideStatus.DriverArrived or RideStatus.PassengerOnBoard))
        {
            return Result.Failure(NotCancellableByDriverError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var cancelled = await _rideRepository.TryTransitionAsync(
            ride.Id, ride.Status, RideStatus.CancelledByDriver, command.RequestingUserId,
            $"{command.Reason}: {command.Details}", utcNow, cancellationToken);

        if (!cancelled)
        {
            return Result.Failure(NotCancellableByDriverError, ErrorType.Conflict);
        }

        driver.SetAvailability(DriverAvailabilityStatus.Available, utcNow);
        await _driverRepository.UpdateAsync(driver, cancellationToken);

        return Result.Success();
    }
}
