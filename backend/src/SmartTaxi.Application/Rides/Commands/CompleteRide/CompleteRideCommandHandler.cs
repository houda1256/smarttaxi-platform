using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CompleteRide;

/// <summary>
/// Idempotent: calling this a second time on an already-completed Ride is a
/// harmless no-op success, never a duplicate fare calculation or a second
/// driver-availability release. No financial ledger entry is created here —
/// this only prepares the FinalFare field as the clean integration point for
/// the future Payments module (see RideCompleted, documented, never raised).
/// </summary>
public sealed class CompleteRideCommandHandler : ICommandHandler<CompleteRideCommand, Result<decimal>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotInProgressError = "Cette course n'est pas en cours.";
    private const string VehicleNotFoundError = "Véhicule introuvable pour cette course.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IFareCalculator _fareCalculator;
    private readonly IDynamicPricingProvider _dynamicPricingProvider;

    public CompleteRideCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IVehicleRepository vehicleRepository,
        IFareCalculator fareCalculator, IDynamicPricingProvider dynamicPricingProvider)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _fareCalculator = fareCalculator;
        _dynamicPricingProvider = dynamicPricingProvider;
    }

    public async Task<Result<decimal>> Handle(CompleteRideCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.SelectedDriverId is null)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

        if (driver is null || driver.UserId != command.RequestingUserId)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.Status is RideStatus.AwaitingPayment or RideStatus.Completed)
        {
            // Already completed — idempotent no-op, return the fare already recorded.
            return Result<decimal>.Success(ride.FinalFare ?? 0m);
        }

        if (ride.Status != RideStatus.InProgress)
        {
            return Result<decimal>.Failure(NotInProgressError, ErrorType.Conflict);
        }

        decimal finalFare;

        if (ride.NegotiatedFinalFare is not null)
        {
            // An accepted fare negotiation is immutable — never recomputed via the standard calculator.
            finalFare = ride.NegotiatedFinalFare.Value;
        }
        else
        {
            var vehicle = ride.VehicleId is null ? null : await _vehicleRepository.GetByIdAsync(ride.VehicleId.Value, cancellationToken);

            if (vehicle is null)
            {
                return Result<decimal>.Failure(VehicleNotFoundError, ErrorType.NotFound);
            }

            var dynamicPricing = _dynamicPricingProvider.GetMultiplier(DateTime.UtcNow, null);
            var fare = _fareCalculator.Calculate(new FareCalculationInput(
                command.ActualDistanceKm, command.ActualDurationMinutes, vehicle.VehicleCategory, dynamicPricing.Multiplier));
            finalFare = fare.TotalFare;
        }

        var utcNow = DateTime.UtcNow;
        var completed = await _rideRepository.TryCompleteAsync(
            ride.Id, command.ActualDistanceKm, command.ActualDurationMinutes, finalFare, utcNow, cancellationToken);

        if (!completed)
        {
            // Lost a race with another completion attempt — treat as the same idempotent success.
            var reloaded = await _rideRepository.GetByIdAsync(ride.Id, cancellationToken);
            return Result<decimal>.Success(reloaded?.FinalFare ?? finalFare);
        }

        driver.SetAvailability(DriverAvailabilityStatus.Available, utcNow);
        await _driverRepository.UpdateAsync(driver, cancellationToken);

        return Result<decimal>.Success(finalFare);
    }
}
