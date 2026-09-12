using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CancelRideByCustomer;

/// <summary>
/// Fee escalates by stage: free before Driver acceptance, a configurable fee
/// after acceptance, a higher configurable fee after arrival. Cancellation is
/// not allowed once the Ride has actually started — no financial charge is
/// created here, only the fee amount that a future Payments integration
/// would charge is returned.
/// </summary>
public sealed class CancelRideByCustomerCommandHandler : ICommandHandler<CancelRideByCustomerCommand, Result<decimal>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string AlreadyTerminalError = "Cette course est déjà terminée ou annulée.";
    private const string CannotCancelAfterStartError = "Une course en cours ne peut plus être annulée.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IDriverReservationHoldRepository _holdRepository;
    private readonly IFarePricingPolicy _pricingPolicy;

    public CancelRideByCustomerCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository,
        IDriverReservationHoldRepository holdRepository, IFarePricingPolicy pricingPolicy)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _holdRepository = holdRepository;
        _pricingPolicy = pricingPolicy;
    }

    public async Task<Result<decimal>> Handle(CancelRideByCustomerCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (RideStatusTransitions.IsTerminal(ride.Status))
        {
            return Result<decimal>.Failure(AlreadyTerminalError, ErrorType.Conflict);
        }

        if (ride.Status is RideStatus.InProgress or RideStatus.AwaitingPayment)
        {
            return Result<decimal>.Failure(CannotCancelAfterStartError, ErrorType.Conflict);
        }

        var fee = ride.Status switch
        {
            RideStatus.DriverAccepted or RideStatus.DriverEnRoute => _pricingPolicy.CancellationFeeAfterAcceptance,
            RideStatus.DriverArrived or RideStatus.PassengerOnBoard => _pricingPolicy.CancellationFeeAfterArrival,
            _ => 0m
        };

        var utcNow = DateTime.UtcNow;
        var cancelled = await _rideRepository.TryTransitionAsync(
            ride.Id, ride.Status, RideStatus.CancelledByCustomer, command.RequestingUserId,
            $"{command.Reason}: {command.Details}", utcNow, cancellationToken);

        if (!cancelled)
        {
            return Result<decimal>.Failure(AlreadyTerminalError, ErrorType.Conflict);
        }

        var activeHold = await _holdRepository.GetActiveForRideAsync(ride.Id, cancellationToken);

        if (activeHold is not null)
        {
            await _holdRepository.TryReleaseAsync(activeHold.Id, utcNow, cancellationToken);
        }

        await ReleaseDriverIfAssignedAsync(ride.SelectedDriverId, utcNow, cancellationToken);

        return Result<decimal>.Success(fee);
    }

    private async Task ReleaseDriverIfAssignedAsync(Guid? driverId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (driverId is null)
        {
            return;
        }

        var driver = await _driverRepository.GetByIdAsync(driverId.Value, cancellationToken);

        if (driver is not null && driver.AvailabilityStatus != DriverAvailabilityStatus.Offline)
        {
            driver.SetAvailability(DriverAvailabilityStatus.Available, utcNow);
            await _driverRepository.UpdateAsync(driver, cancellationToken);
        }
    }
}
