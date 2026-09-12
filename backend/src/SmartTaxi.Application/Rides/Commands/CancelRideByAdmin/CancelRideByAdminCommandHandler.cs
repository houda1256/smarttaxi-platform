using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CancelRideByAdmin;

/// <summary>An admin override is available from any non-terminal Ride status — no ownership check, gated purely by the rides.cancel.admin permission at the API layer.</summary>
public sealed class CancelRideByAdminCommandHandler : ICommandHandler<CancelRideByAdminCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string AlreadyTerminalError = "Cette course est déjà terminée ou annulée.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverReservationHoldRepository _holdRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public CancelRideByAdminCommandHandler(
        IRideRepository rideRepository, IDriverReservationHoldRepository holdRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _holdRepository = holdRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(CancelRideByAdminCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (!RideStatusTransitions.CanTransition(ride.Status, RideStatus.CancelledByAdmin))
        {
            return Result.Failure(AlreadyTerminalError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var cancelled = await _rideRepository.TryTransitionAsync(
            ride.Id, ride.Status, RideStatus.CancelledByAdmin, command.AdminUserId, command.Reason, utcNow, cancellationToken);

        if (!cancelled)
        {
            return Result.Failure(AlreadyTerminalError, ErrorType.Conflict);
        }

        var activeHold = await _holdRepository.GetActiveForRideAsync(ride.Id, cancellationToken);

        if (activeHold is not null)
        {
            await _holdRepository.TryReleaseAsync(activeHold.Id, utcNow, cancellationToken);
        }

        if (ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

            if (driver is not null && driver.AvailabilityStatus != DriverAvailabilityStatus.Offline)
            {
                driver.SetAvailability(DriverAvailabilityStatus.Available, utcNow);
                await _driverRepository.UpdateAsync(driver, cancellationToken);
            }
        }

        return Result.Success();
    }
}
