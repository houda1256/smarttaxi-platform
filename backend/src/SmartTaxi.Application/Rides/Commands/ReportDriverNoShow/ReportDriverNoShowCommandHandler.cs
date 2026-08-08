using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.ReportDriverNoShow;

/// <summary>Reportable once the Driver has accepted but never arrived — cascades the Ride back to DriversAvailable so the Customer can pick a different Driver, and never silently reassigns one.</summary>
public sealed class ReportDriverNoShowCommandHandler : ICommandHandler<ReportDriverNoShowCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotEligibleError = "Cette course n'est pas éligible à un signalement d'absence du chauffeur.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public ReportDriverNoShowCommandHandler(IRideRepository rideRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(ReportDriverNoShowCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.Status is not (RideStatus.DriverAccepted or RideStatus.DriverEnRoute))
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var transitioned = await _rideRepository.TryTransitionAsync(
            ride.Id, ride.Status, RideStatus.DriverNoShow, command.RequestingUserId, null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        if (ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

            if (driver is not null)
            {
                driver.SetAvailability(DriverAvailabilityStatus.Available, utcNow);
                await _driverRepository.UpdateAsync(driver, cancellationToken);
            }
        }

        await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.DriverNoShow, RideStatus.DriversAvailable, null, null, utcNow, cancellationToken);

        return Result.Success();
    }
}
