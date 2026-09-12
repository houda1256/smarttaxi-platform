using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.ReportCustomerNoShow;
using SmartTaxi.Application.Rides.Commands.ReportDriverNoShow;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class NoShowCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeRideServicePolicy _servicePolicy = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly ReportCustomerNoShowCommandHandler _customerNoShowHandler;
    private readonly ReportDriverNoShowCommandHandler _driverNoShowHandler;

    public NoShowCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _customerNoShowHandler = new ReportCustomerNoShowCommandHandler(_rideRepository, _driverRepository, _servicePolicy);
        _driverNoShowHandler = new ReportDriverNoShowCommandHandler(_rideRepository, _driverRepository);
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
    public async Task CustomerNoShow_BeforeWaitingPeriodElapsed_ReturnsValidationError()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryMarkDriverArrivedAsync(rideId, DateTime.UtcNow, CancellationToken.None);

        var result = await _customerNoShowHandler.Handle(new ReportCustomerNoShowCommand(driverUserId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CustomerNoShow_AfterWaitingPeriodElapsed_Succeeds()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryMarkDriverArrivedAsync(rideId, DateTime.UtcNow.AddMinutes(-10), CancellationToken.None);

        var result = await _customerNoShowHandler.Handle(new ReportCustomerNoShowCommand(driverUserId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.CustomerNoShow, ride!.Status);
    }

    [Fact]
    public async Task CustomerNoShow_BeforeDriverArrived_ReturnsConflict()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();

        var result = await _customerNoShowHandler.Handle(new ReportCustomerNoShowCommand(driverUserId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task DriverNoShow_ReportedByCustomer_CascadesBackToDriversAvailable()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _driverNoShowHandler.Handle(new ReportDriverNoShowCommand(customerId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.DriversAvailable, ride!.Status);
    }

    [Fact]
    public async Task DriverNoShow_ByUnrelatedUser_ReturnsNotFound()
    {
        var (_, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _driverNoShowHandler.Handle(new ReportDriverNoShowCommand(Guid.NewGuid(), rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
