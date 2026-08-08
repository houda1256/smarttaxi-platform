using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.DriverRejectRide;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class DriverResponseCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly DriverRejectRideCommandHandler _rejectHandler;

    public DriverResponseCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _rejectHandler = new DriverRejectRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId, DriverProfile Driver)> CreatePendingResponseRideAsync()
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

        var reloadedDriver = (await _driverRepository.GetByIdAsync(driver.Id, CancellationToken.None))!;
        return (customerId, rideId, driverUserId, reloadedDriver);
    }

    [Fact]
    public async Task Accept_ByOwningDriver_MovesRideToDriverAcceptedAndSetsAvailabilityOnRide()
    {
        var (_, rideId, driverUserId, _) = await CreatePendingResponseRideAsync();

        var result = await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.DriverAccepted, ride!.Status);
        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId!.Value, CancellationToken.None);
        Assert.Equal(DriverAvailabilityStatus.OnRide, driver!.AvailabilityStatus);
    }

    [Fact]
    public async Task Accept_ByAnotherDriver_ReturnsNotFound()
    {
        var (_, rideId, _, _) = await CreatePendingResponseRideAsync();

        var result = await _acceptHandler.Handle(new DriverAcceptRideCommand(Guid.NewGuid(), rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Accept_WithExpiredHold_FailsAndReturnsRideToDriversAvailable()
    {
        var (_, rideId, driverUserId, driver) = await CreatePendingResponseRideAsync();
        var hold = await _holdRepository.GetActiveForRideAsync(rideId, CancellationToken.None);
        typeof(DriverReservationHold).GetProperty(nameof(DriverReservationHold.ExpiresAt))!
            .SetValue(hold, DateTime.UtcNow.AddSeconds(-1));

        var result = await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.DriversAvailable, ride!.Status);
    }

    [Fact]
    public async Task Reject_ByOwningDriver_ReleasesHoldAndReturnsRideToDriversAvailable()
    {
        var (_, rideId, driverUserId, _) = await CreatePendingResponseRideAsync();

        var result = await _rejectHandler.Handle(new DriverRejectRideCommand(driverUserId, rideId, "Trop loin"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.DriversAvailable, ride!.Status);
        var hold = await _holdRepository.GetActiveForRideAsync(rideId, CancellationToken.None);
        Assert.Null(hold);
    }

    [Fact]
    public async Task Accept_AfterAlreadyAccepted_ReturnsConflict()
    {
        var (_, rideId, driverUserId, _) = await CreatePendingResponseRideAsync();
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        var result = await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
