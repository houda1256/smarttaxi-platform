using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.DriverAcceptRide;

/// <summary>
/// An expired hold can never be accepted — this handler checks expiry itself
/// (no background scheduler exists) and, on finding one, auto-cascades the
/// Ride back through DriverRejected to DriversAvailable so the Customer can
/// pick another Driver, exactly as an explicit rejection would.
/// </summary>
public sealed class DriverAcceptRideCommandHandler : ICommandHandler<DriverAcceptRideCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NoActiveHoldError = "Aucune réservation active pour cette course.";
    private const string HoldExpiredError = "Le délai de réponse a expiré.";
    private const string NotPendingError = "Cette course n'est plus en attente de réponse du chauffeur.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverReservationHoldRepository _holdRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public DriverAcceptRideCommandHandler(
        IRideRepository rideRepository, IDriverReservationHoldRepository holdRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _holdRepository = holdRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(DriverAcceptRideCommand command, CancellationToken cancellationToken)
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

        if (hold.IsExpired(utcNow))
        {
            await _holdRepository.TryExpireAsync(hold.Id, utcNow, cancellationToken);
            await CascadeToDriversAvailableAsync(ride.Id, "Délai de réponse expiré", utcNow, cancellationToken);
            return Result.Failure(HoldExpiredError, ErrorType.Conflict);
        }

        var accepted = await _holdRepository.TryAcceptAsync(hold.Id, utcNow, cancellationToken);

        if (!accepted)
        {
            return Result.Failure(NoActiveHoldError, ErrorType.Conflict);
        }

        var transitioned = await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.PendingDriverResponse, RideStatus.DriverAccepted, command.RequestingUserId, null, utcNow,
            cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        driver.SetAvailability(DriverAvailabilityStatus.OnRide, utcNow);
        await _driverRepository.UpdateAsync(driver, cancellationToken);

        return Result.Success();
    }

    private async Task CascadeToDriversAvailableAsync(Guid rideId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.PendingDriverResponse, RideStatus.DriverRejected, null, reason, utcNow, cancellationToken);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverRejected, RideStatus.DriversAvailable, null, null, utcNow, cancellationToken);
    }
}
