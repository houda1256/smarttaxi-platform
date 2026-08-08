using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.ReportCustomerNoShow;

/// <summary>Only reportable once the Driver has reached DriverArrived and the configurable waiting period has actually elapsed — location/timing evidence is DriverArrivedAt, already preserved on the Ride.</summary>
public sealed class ReportCustomerNoShowCommandHandler : ICommandHandler<ReportCustomerNoShowCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotArrivedError = "Le chauffeur doit être arrivé pour signaler une absence du client.";
    private const string WaitingPeriodNotElapsedError = "Le délai d'attente n'est pas encore écoulé.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideServicePolicy _servicePolicy;

    public ReportCustomerNoShowCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IRideServicePolicy servicePolicy)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _servicePolicy = servicePolicy;
    }

    public async Task<Result> Handle(ReportCustomerNoShowCommand command, CancellationToken cancellationToken)
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

        if (ride.Status != RideStatus.DriverArrived || ride.DriverArrivedAt is null)
        {
            return Result.Failure(NotArrivedError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var waitingDeadline = ride.DriverArrivedAt.Value.AddMinutes(_servicePolicy.CustomerNoShowWaitingMinutes);

        if (utcNow < waitingDeadline)
        {
            return Result.Failure(WaitingPeriodNotElapsedError, ErrorType.Validation);
        }

        var transitioned = await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.DriverArrived, RideStatus.CustomerNoShow, command.RequestingUserId, null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotArrivedError, ErrorType.Conflict);
        }

        driver.SetAvailability(DriverAvailabilityStatus.Available, utcNow);
        await _driverRepository.UpdateAsync(driver, cancellationToken);

        return Result.Success();
    }
}
