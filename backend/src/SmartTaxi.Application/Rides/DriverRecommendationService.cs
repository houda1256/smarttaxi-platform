using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Drivers;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides;

/// <summary>
/// Explainable, weighted, deterministic — never ML. Eligibility (approved,
/// available, no active ride, valid active assignment, eligible vehicle,
/// category/capacity/accessibility match) is checked as hard gates before any
/// scoring happens; a subscription bonus, when one exists in the future, may
/// only ever be additive on top of a Driver that already passed every gate.
/// Reuses Fleet's own repositories and VehicleEligibilityChecker directly —
/// none of this eligibility logic is duplicated.
/// </summary>
public sealed class DriverRecommendationService : IDriverRecommendationService
{
    private const decimal DistanceWeight = 2m;
    private const decimal MaxDistanceScoreKm = 20m;
    private const decimal RatingWeight = 10m;
    private const int ExperienceCapRides = 100;
    private const decimal ExperienceWeight = 20m;

    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverVehicleAssignmentRepository _assignmentRepository;
    private readonly IRideRepository _rideRepository;
    private readonly VehicleEligibilityChecker _vehicleEligibilityChecker;
    private readonly IRouteEstimationService _routeEstimationService;
    private readonly IDriverSearchPolicy _searchPolicy;

    public DriverRecommendationService(
        IDriverProfileRepository driverRepository, IVehicleRepository vehicleRepository,
        IDriverVehicleAssignmentRepository assignmentRepository, IRideRepository rideRepository,
        VehicleEligibilityChecker vehicleEligibilityChecker, IRouteEstimationService routeEstimationService,
        IDriverSearchPolicy searchPolicy)
    {
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _assignmentRepository = assignmentRepository;
        _rideRepository = rideRepository;
        _vehicleEligibilityChecker = vehicleEligibilityChecker;
        _routeEstimationService = routeEstimationService;
        _searchPolicy = searchPolicy;
    }

    public async Task<IReadOnlyCollection<DriverRecommendationResult>> GetRecommendationsAsync(
        DriverRecommendationCriteria criteria, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var candidates = await _driverRepository.GetEligibleAsync(cancellationToken);
        var results = new List<DriverRecommendationResult>();

        foreach (var radiusKm in _searchPolicy.ProgressiveSearchRadiusKm)
        {
            results.Clear();

            foreach (var driver in candidates)
            {
                var result = await TryScoreAsync(driver, criteria, radiusKm, utcNow, cancellationToken);

                if (result is not null)
                {
                    results.Add(result);
                }
            }

            if (results.Count > 0)
            {
                break;
            }
        }

        return results.OrderByDescending(r => r.RecommendationScore).ToList();
    }

    private async Task<DriverRecommendationResult?> TryScoreAsync(
        DriverProfile driver, DriverRecommendationCriteria criteria, int radiusKm, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (driver.LastKnownLatitude is null || driver.LastKnownLongitude is null)
        {
            return null;
        }

        if (await _rideRepository.GetActiveForDriverAsync(driver.Id, cancellationToken) is not null)
        {
            return null;
        }

        var assignments = await _assignmentRepository.GetActiveOrPendingForDriverAsync(driver.Id, null, cancellationToken);
        var activeAssignment = assignments.FirstOrDefault(a => a.Status == AssignmentStatus.Active);

        if (activeAssignment is null)
        {
            return null;
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(activeAssignment.VehicleId, cancellationToken);

        if (vehicle is null || !vehicle.IsCurrentlyEligibleForRides())
        {
            return null;
        }

        var vehicleEligibility = await _vehicleEligibilityChecker.CheckAsync(vehicle, utcNow, cancellationToken);

        if (!vehicleEligibility.IsEligible)
        {
            return null;
        }

        if (criteria.PreferredVehicleCategory is not null && vehicle.VehicleCategory != criteria.PreferredVehicleCategory)
        {
            return null;
        }

        if (vehicle.SeatCount < criteria.PassengerCount)
        {
            return null;
        }

        if (criteria.NeedsAccessibleVehicle && !vehicle.IsAccessible)
        {
            return null;
        }

        if (criteria.NeedsAirConditioning && !vehicle.HasAirConditioning)
        {
            return null;
        }

        var driverLocation = GeoCoordinate.Create(driver.LastKnownLatitude.Value, driver.LastKnownLongitude.Value);
        var route = _routeEstimationService.Estimate(driverLocation, criteria.PickupLocation);

        if (route.DistanceKm > radiusKm)
        {
            return null;
        }

        var reasons = new List<string>();
        var score = ComputeScore(driver, route.DistanceKm, reasons);

        return new DriverRecommendationResult(
            driver.Id, vehicle.Id, DriverProfileSummary.FromEntity(driver), VehicleSummary.FromEntity(vehicle),
            route.DistanceKm, route.DurationMinutes, score, reasons);
    }

    private static decimal ComputeScore(DriverProfile driver, decimal distanceKm, List<string> reasons)
    {
        var score = 0m;

        var distanceScore = Math.Max(0, MaxDistanceScoreKm - distanceKm) * DistanceWeight;
        score += distanceScore;

        if (distanceKm <= 2)
        {
            reasons.Add("Très proche du point de prise en charge");
        }

        var ratingScore = driver.AverageRating * RatingWeight;
        score += ratingScore;

        if (driver.AverageRating >= 4.5m)
        {
            reasons.Add("Excellente note moyenne");
        }

        var experienceScore = Math.Min(driver.CompletedRideCount, ExperienceCapRides) / (decimal)ExperienceCapRides * ExperienceWeight;
        score += experienceScore;

        if (driver.CompletedRideCount >= ExperienceCapRides)
        {
            reasons.Add("Chauffeur expérimenté");
        }

        if (driver.CancellationCount == 0)
        {
            reasons.Add("Aucune annulation récente");
        }

        return Math.Round(score, 2);
    }
}
