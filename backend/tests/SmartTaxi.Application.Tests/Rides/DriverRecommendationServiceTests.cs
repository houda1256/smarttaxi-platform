using SmartTaxi.Application.Fleet.Vehicles;
using SmartTaxi.Application.Rides;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Policies;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Tests.Rides;

public class DriverRecommendationServiceTests
{
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverVehicleAssignmentRepository _assignmentRepository = new();
    private readonly FakeVehicleDocumentRepository _documentRepository = new();
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRouteEstimationService _routeEstimationService = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly DriverRecommendationService _service;

    public DriverRecommendationServiceTests()
    {
        _service = new DriverRecommendationService(
            _driverRepository, _vehicleRepository, _assignmentRepository, _rideRepository,
            new VehicleEligibilityChecker(_documentRepository), _routeEstimationService, _searchPolicy);
    }

    private async Task<(DriverProfile Driver, Vehicle Vehicle)> CreateEligibleDriverAsync(
        double latitude, double longitude, VehicleCategory category = VehicleCategory.Standard, int seatCount = 5,
        bool isAccessible = false, bool hasAirConditioning = true,
        IReadOnlyCollection<VehicleDocumentType>? skipDocumentTypes = null)
    {
        var driver = DriverProfile.Create(Guid.NewGuid(), $"LIC-{Guid.NewGuid():N}", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);

        var reloadedDriver = (await _driverRepository.GetByIdAsync(driver.Id, CancellationToken.None))!;
        reloadedDriver.SetAvailability(DriverAvailabilityStatus.Available, DateTime.UtcNow);
        reloadedDriver.UpdateLastKnownLocation(latitude, longitude, DateTime.UtcNow);
        await _driverRepository.UpdateAsync(reloadedDriver, CancellationToken.None);

        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", $"PL-{Guid.NewGuid():N}"[..10], null, 0,
            FuelType.Petrol, TransmissionType.Automatic, seatCount, hasAirConditioning, isAccessible, category, null,
            DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        foreach (var type in VehicleDocumentRequirements.Critical)
        {
            if (skipDocumentTypes is not null && skipDocumentTypes.Contains(type))
            {
                continue;
            }

            var document = VehicleDocument.Upload(vehicle.Id, type, "key", "f.pdf", "application/pdf", 1, $"hash-{Guid.NewGuid()}", null, null, DateTime.UtcNow);
            await _documentRepository.AddAsync(document, CancellationToken.None);
            await _documentRepository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        }

        var assignment = DriverVehicleAssignment.CreateDraft(
            reloadedDriver.Id, vehicle.Id, Guid.NewGuid(), new DateOnly(2026, 1, 1), null, null, null, DaysOfWeek.All,
            Guid.NewGuid(), DateTime.UtcNow);
        typeof(DriverVehicleAssignment).GetProperty(nameof(DriverVehicleAssignment.Status))!.SetValue(assignment, AssignmentStatus.Active);
        await _assignmentRepository.AddAsync(assignment, CancellationToken.None);

        return (reloadedDriver, vehicle);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ForFullyEligibleDriver_ReturnsOneResult()
    {
        await CreateEligibleDriverAsync(36.8065, 10.1815);
        var criteria = new DriverRecommendationCriteria(GeoCoordinate.Create(36.807, 10.182), null, 1, false, false);

        var results = await _service.GetRecommendationsAsync(criteria, CancellationToken.None);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ForDriverWithoutKnownLocation_ExcludesDriver()
    {
        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC-X", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        var reloaded = (await _driverRepository.GetByIdAsync(driver.Id, CancellationToken.None))!;
        reloaded.SetAvailability(DriverAvailabilityStatus.Available, DateTime.UtcNow);
        await _driverRepository.UpdateAsync(reloaded, CancellationToken.None);

        var criteria = new DriverRecommendationCriteria(GeoCoordinate.Create(36.807, 10.182), null, 1, false, false);
        var results = await _service.GetRecommendationsAsync(criteria, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ForVehicleWithExpiredCriticalDocument_ExcludesDriver()
    {
        var (_, vehicle) = await CreateEligibleDriverAsync(36.8065, 10.1815, skipDocumentTypes: [VehicleDocumentType.Insurance]);

        var expiredInsurance = VehicleDocument.Upload(
            vehicle.Id, VehicleDocumentType.Insurance, "key2", "f2.pdf", "application/pdf", 1, "hash-expired",
            null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-30));
        await _documentRepository.AddAsync(expiredInsurance, CancellationToken.None);
        await _documentRepository.TryApproveAsync(expiredInsurance.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), null, CancellationToken.None);

        var criteria = new DriverRecommendationCriteria(GeoCoordinate.Create(36.807, 10.182), null, 1, false, false);
        var results = await _service.GetRecommendationsAsync(criteria, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ForDriverWithActiveRide_ExcludesDriver()
    {
        var (driver, _) = await CreateEligibleDriverAsync(36.8065, 10.1815);
        var ride = Domain.Rides.Entities.Ride.Create(
            Guid.NewGuid(), Domain.Rides.Enums.RideType.Immediate, "A", GeoCoordinate.Create(36.8, 10.1), "B",
            GeoCoordinate.Create(36.9, 10.3), null, 1, 0, false, false, false, false, null,
            Domain.Rides.Enums.RidePaymentMethod.Cash, null, "TND", DateTime.UtcNow);
        await _rideRepository.AddAsync(ride, CancellationToken.None);
        typeof(Domain.Rides.Entities.Ride).GetProperty(nameof(Domain.Rides.Entities.Ride.SelectedDriverId))!.SetValue(ride, driver.Id);
        typeof(Domain.Rides.Entities.Ride).GetProperty(nameof(Domain.Rides.Entities.Ride.Status))!
            .SetValue(ride, Domain.Rides.Enums.RideStatus.InProgress);

        var criteria = new DriverRecommendationCriteria(GeoCoordinate.Create(36.807, 10.182), null, 1, false, false);
        var results = await _service.GetRecommendationsAsync(criteria, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ForInsufficientSeatCount_ExcludesDriver()
    {
        await CreateEligibleDriverAsync(36.8065, 10.1815, seatCount: 2);
        var criteria = new DriverRecommendationCriteria(GeoCoordinate.Create(36.807, 10.182), null, 4, false, false);

        var results = await _service.GetRecommendationsAsync(criteria, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetRecommendationsAsync_RanksCloserDriverHigher()
    {
        await CreateEligibleDriverAsync(36.8065, 10.1815);
        await CreateEligibleDriverAsync(36.815, 10.182);
        var criteria = new DriverRecommendationCriteria(GeoCoordinate.Create(36.807, 10.182), null, 1, false, false);

        var results = await _service.GetRecommendationsAsync(criteria, CancellationToken.None);

        Assert.Equal(2, results.Count);
        var ordered = results.OrderByDescending(r => r.RecommendationScore).ToList();
        Assert.True(ordered[0].DistanceToPickupKm < ordered[1].DistanceToPickupKm);
    }
}
