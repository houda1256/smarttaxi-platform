using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetRecommendedDrivers;

/// <summary>
/// Reads back the stable snapshot written by SearchDrivers and enriches it
/// with current Driver/Vehicle/preferences data and a full trip fare
/// estimate (pickup→destination, not the driver's distance to pickup).
/// </summary>
public sealed class GetRecommendedDriversQueryHandler
    : IQueryHandler<GetRecommendedDriversQuery, Result<IReadOnlyCollection<RideDriverRecommendationSummary>>>
{
    private const string NotFoundError = "Course introuvable.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideDriverRecommendationRepository _recommendationRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IRouteEstimationService _routeEstimationService;
    private readonly IFareCalculator _fareCalculator;
    private readonly IDynamicPricingProvider _dynamicPricingProvider;

    public GetRecommendedDriversQueryHandler(
        IRideRepository rideRepository, IRideDriverRecommendationRepository recommendationRepository,
        IDriverProfileRepository driverRepository, IVehicleRepository vehicleRepository,
        IUserPreferencesRepository preferencesRepository, IRouteEstimationService routeEstimationService,
        IFareCalculator fareCalculator, IDynamicPricingProvider dynamicPricingProvider)
    {
        _rideRepository = rideRepository;
        _recommendationRepository = recommendationRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _preferencesRepository = preferencesRepository;
        _routeEstimationService = routeEstimationService;
        _fareCalculator = fareCalculator;
        _dynamicPricingProvider = dynamicPricingProvider;
    }

    public async Task<Result<IReadOnlyCollection<RideDriverRecommendationSummary>>> Handle(
        GetRecommendedDriversQuery query, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(query.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != query.RequestingUserId)
        {
            return Result<IReadOnlyCollection<RideDriverRecommendationSummary>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var recommendations = await _recommendationRepository.GetForRideAsync(query.RideId, cancellationToken);
        var tripRoute = _routeEstimationService.Estimate(ride.PickupLocation, ride.DestinationLocation);
        var dynamicPricing = _dynamicPricingProvider.GetMultiplier(DateTime.UtcNow, null);

        var summaries = new List<RideDriverRecommendationSummary>();

        foreach (var recommendation in recommendations.OrderBy(r => r.Rank))
        {
            var driver = await _driverRepository.GetByIdAsync(recommendation.DriverId, cancellationToken);
            var vehicle = await _vehicleRepository.GetByIdAsync(recommendation.VehicleId, cancellationToken);

            if (driver is null || vehicle is null)
            {
                continue;
            }

            var preferences = await _preferencesRepository.GetByUserIdAsync(driver.UserId, cancellationToken);
            var fare = _fareCalculator.Calculate(
                new FareCalculationInput(tripRoute.DistanceKm, tripRoute.DurationMinutes, vehicle.VehicleCategory, dynamicPricing.Multiplier));

            summaries.Add(new RideDriverRecommendationSummary(
                driver.Id, vehicle.Id, preferences?.DisplayName, preferences?.AvatarUrl, driver.AverageRating,
                driver.CompletedRideCount, recommendation.DistanceToPickupKm, recommendation.EstimatedArrivalMinutes,
                vehicle.Brand, vehicle.Model, vehicle.VehicleCategory.ToString(), vehicle.LicensePlate,
                vehicle.HasAirConditioning, vehicle.IsAccessible, vehicle.SeatCount, fare.TotalFare,
                recommendation.RecommendationScore,
                recommendation.RecommendationReasons.Split("; ", StringSplitOptions.RemoveEmptyEntries), recommendation.Rank));
        }

        return Result<IReadOnlyCollection<RideDriverRecommendationSummary>>.Success(summaries);
    }
}
