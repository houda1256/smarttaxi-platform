using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Queries.GetPublicRideShareView;

public sealed class GetPublicRideShareViewQueryHandler
    : IQueryHandler<GetPublicRideShareViewQuery, Result<PublicRideShareView>>
{
    private const string InvalidTokenError = "Lien de partage invalide ou expiré.";

    private readonly IRideShareTokenRepository _tokenRepository;
    private readonly IRideShareTokenHasher _tokenHasher;
    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IRouteEstimationService _routeEstimationService;

    public GetPublicRideShareViewQueryHandler(
        IRideShareTokenRepository tokenRepository, IRideShareTokenHasher tokenHasher, IRideRepository rideRepository,
        IDriverProfileRepository driverRepository, IVehicleRepository vehicleRepository,
        IUserPreferencesRepository preferencesRepository, IRouteEstimationService routeEstimationService)
    {
        _tokenRepository = tokenRepository;
        _tokenHasher = tokenHasher;
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _preferencesRepository = preferencesRepository;
        _routeEstimationService = routeEstimationService;
    }

    public async Task<Result<PublicRideShareView>> Handle(GetPublicRideShareViewQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.RawToken))
        {
            return Result<PublicRideShareView>.Failure(InvalidTokenError, ErrorType.NotFound);
        }

        var tokenHash = _tokenHasher.Hash(query.RawToken);
        var token = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (token is null || !token.IsValid(utcNow))
        {
            return Result<PublicRideShareView>.Failure(InvalidTokenError, ErrorType.NotFound);
        }

        var ride = await _rideRepository.GetByIdAsync(token.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<PublicRideShareView>.Failure(InvalidTokenError, ErrorType.NotFound);
        }

        string? driverDisplayName = null;
        string? vehicleBrand = null;
        string? vehicleModel = null;
        string? vehicleLicensePlate = null;

        if (ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

            if (driver is not null)
            {
                var preferences = await _preferencesRepository.GetByUserIdAsync(driver.UserId, cancellationToken);
                driverDisplayName = preferences?.DisplayName;
            }
        }

        if (ride.VehicleId is not null)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(ride.VehicleId.Value, cancellationToken);

            if (vehicle is not null)
            {
                vehicleBrand = vehicle.Brand;
                vehicleModel = vehicle.Model;
                vehicleLicensePlate = vehicle.LicensePlate;
            }
        }

        int? estimatedArrivalMinutes = null;

        if (ride.LastKnownLatitude is not null && ride.LastKnownLongitude is not null)
        {
            var current = GeoCoordinate.Create(ride.LastKnownLatitude.Value, ride.LastKnownLongitude.Value);
            var target = ride.Status == RideStatus.InProgress ? ride.DestinationLocation : ride.PickupLocation;
            estimatedArrivalMinutes = _routeEstimationService.Estimate(current, target).DurationMinutes;
        }

        var view = new PublicRideShareView(
            ride.Status.ToString(), ride.LastKnownLatitude, ride.LastKnownLongitude, driverDisplayName,
            vehicleBrand, vehicleModel, vehicleLicensePlate, estimatedArrivalMinutes);

        return Result<PublicRideShareView>.Success(view);
    }
}
