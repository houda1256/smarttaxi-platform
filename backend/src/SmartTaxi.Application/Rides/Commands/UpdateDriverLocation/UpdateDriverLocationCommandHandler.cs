using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Commands.UpdateDriverLocation;

/// <summary>
/// Only the Ride's own selected Driver may push location for that Ride, and
/// only while the Ride is in a status where tracking is meaningful. Points
/// are rate-limited (IRideServicePolicy.MinLocationUpdateIntervalSeconds) so
/// a misbehaving client can't flood storage; retention/pruning is a separate,
/// on-demand concern (IRideLocationPointRepository.PruneOlderThanAsync).
/// </summary>
public sealed class UpdateDriverLocationCommandHandler : ICommandHandler<UpdateDriverLocationCommand, Result>
{
    private static readonly RideStatus[] TrackableStatuses =
    [
        RideStatus.DriverEnRoute, RideStatus.DriverArrived, RideStatus.PassengerOnBoard, RideStatus.InProgress
    ];

    private const string NotFoundError = "Course introuvable.";
    private const string NotTrackableError = "La course n'est pas dans un état permettant le suivi de position.";
    private const string TooFrequentError = "Mise à jour de position trop fréquente.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideLocationPointRepository _locationPointRepository;
    private readonly IRideServicePolicy _servicePolicy;
    private readonly IRideRealtimeNotifier _realtimeNotifier;

    public UpdateDriverLocationCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository,
        IRideLocationPointRepository locationPointRepository, IRideServicePolicy servicePolicy,
        IRideRealtimeNotifier realtimeNotifier)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _locationPointRepository = locationPointRepository;
        _servicePolicy = servicePolicy;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<Result> Handle(UpdateDriverLocationCommand command, CancellationToken cancellationToken)
    {
        if (!GeoCoordinate.TryCreate(command.Latitude, command.Longitude, out var location, out var error))
        {
            return Result.Failure(error, ErrorType.Validation);
        }

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

        if (!TrackableStatuses.Contains(ride.Status))
        {
            return Result.Failure(NotTrackableError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var latestPoint = await _locationPointRepository.GetLatestForRideAsync(ride.Id, cancellationToken);

        if (latestPoint is not null
            && (utcNow - latestPoint.RecordedAt).TotalSeconds < _servicePolicy.MinLocationUpdateIntervalSeconds)
        {
            return Result.Failure(TooFrequentError, ErrorType.Validation);
        }

        var point = new RideLocationPoint(
            ride.Id, driver.Id, location, command.Speed, command.Heading, command.Accuracy, utcNow);

        await _locationPointRepository.AddAsync(point, cancellationToken);
        await _rideRepository.TryUpdateLastKnownLocationAsync(ride.Id, location.Latitude, location.Longitude, utcNow, cancellationToken);

        driver.UpdateLastKnownLocation(location.Latitude, location.Longitude, utcNow);
        await _driverRepository.UpdateAsync(driver, cancellationToken);

        await _realtimeNotifier.NotifyLocationUpdatedAsync(ride.Id, location.Latitude, location.Longitude, cancellationToken);

        return Result.Success();
    }
}
