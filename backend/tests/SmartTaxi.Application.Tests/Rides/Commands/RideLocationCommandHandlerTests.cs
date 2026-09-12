using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.UpdateDriverAvailabilityLocation;
using SmartTaxi.Application.Rides.Commands.UpdateDriverLocation;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class RideLocationCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeRideLocationPointRepository _locationPointRepository = new();
    private readonly FakeRideServicePolicy _servicePolicy = new();
    private readonly FakeRideRealtimeNotifier _realtimeNotifier = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly UpdateDriverLocationCommandHandler _updateLocationHandler;
    private readonly UpdateDriverAvailabilityLocationCommandHandler _updateAvailabilityLocationHandler;

    public RideLocationCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _updateLocationHandler = new UpdateDriverLocationCommandHandler(
            _rideRepository, _driverRepository, _locationPointRepository, _servicePolicy, _realtimeNotifier);
        _updateAvailabilityLocationHandler = new UpdateDriverAvailabilityLocationCommandHandler(_driverRepository);
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId)> CreateAcceptedRideAsync()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        var rideId = createResult.Value;

        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);

        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0, FuelType.Petrol,
            TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        return (customerId, rideId, driverUserId);
    }

    [Fact]
    public async Task UpdateLocation_ByOwningDriverWhileEnRoute_Succeeds()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, null, null, DateTime.UtcNow, CancellationToken.None);

        var result = await _updateLocationHandler.Handle(
            new UpdateDriverLocationCommand(driverUserId, rideId, 36.81, 10.15, 30, 90, 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(36.81, ride!.LastKnownLatitude);
        Assert.Single(_realtimeNotifier.LocationNotifications);
    }

    [Fact]
    public async Task UpdateLocation_ByNonOwningDriver_ReturnsNotFound()
    {
        var (_, rideId, _) = await CreateAcceptedRideAsync();
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, null, null, DateTime.UtcNow, CancellationToken.None);

        var result = await _updateLocationHandler.Handle(
            new UpdateDriverLocationCommand(Guid.NewGuid(), rideId, 36.81, 10.15, null, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateLocation_WhenRideNotYetTrackable_ReturnsConflict()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();

        var result = await _updateLocationHandler.Handle(
            new UpdateDriverLocationCommand(driverUserId, rideId, 36.81, 10.15, null, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateLocation_TooFrequent_ReturnsValidationError()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, null, null, DateTime.UtcNow, CancellationToken.None);

        var first = await _updateLocationHandler.Handle(
            new UpdateDriverLocationCommand(driverUserId, rideId, 36.81, 10.15, null, null, null), CancellationToken.None);
        var second = await _updateLocationHandler.Handle(
            new UpdateDriverLocationCommand(driverUserId, rideId, 36.82, 10.16, null, null, null), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Validation, second.ErrorType);
    }

    [Fact]
    public async Task UpdateAvailabilityLocation_UpdatesDriverProfilePosition()
    {
        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);

        var result = await _updateAvailabilityLocationHandler.Handle(
            new UpdateDriverAvailabilityLocationCommand(driverUserId, 36.8, 10.1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _driverRepository.GetByUserIdAsync(driverUserId, CancellationToken.None);
        Assert.Equal(36.8, reloaded!.LastKnownLatitude);
    }
}
