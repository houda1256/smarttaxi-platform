using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.DriverRejectRide;

/// <summary>
/// Never silently assigns another Driver — rejection only releases the hold
/// and returns the Ride to DriversAvailable so the Customer can pick again
/// from their own recommendation list.
/// </summary>
public sealed class DriverRejectRideCommandHandler : ICommandHandler<DriverRejectRideCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NoActiveHoldError = "Aucune réservation active pour cette course.";
    private const string NotPendingError = "Cette course n'est plus en attente de réponse du chauffeur.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverReservationHoldRepository _holdRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public DriverRejectRideCommandHandler(
        IRideRepository rideRepository, IDriverReservationHoldRepository holdRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _holdRepository = holdRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(DriverRejectRideCommand command, CancellationToken cancellationToken)
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

        if (ride.Status != RideStatus.PendingDriverResponse)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var hold = await _holdRepository.GetActiveForRideAsync(ride.Id, cancellationToken);

        if (hold is null)
        {
            return Result.Failure(NoActiveHoldError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var released = await _holdRepository.TryReleaseAsync(hold.Id, utcNow, cancellationToken);

        if (!released)
        {
            return Result.Failure(NoActiveHoldError, ErrorType.Conflict);
        }

        var rejected = await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.PendingDriverResponse, RideStatus.DriverRejected, command.RequestingUserId, command.Reason,
            utcNow, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.DriverRejected, RideStatus.DriversAvailable, null, null, utcNow, cancellationToken);

        return Result.Success();
    }
}
