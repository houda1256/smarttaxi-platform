using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CompleteRide;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.DriverArrived;
using SmartTaxi.Application.Rides.Commands.DriverEnRoute;
using SmartTaxi.Application.Rides.Commands.PassengerOnBoard;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.StartRide;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class DriverLifecycleCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeFareCalculator _fareCalculator = new();
    private readonly FakeDynamicPricingProvider _dynamicPricingProvider = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly DriverEnRouteCommandHandler _enRouteHandler;
    private readonly DriverArrivedCommandHandler _arrivedHandler;
    private readonly PassengerOnBoardCommandHandler _onBoardHandler;
    private readonly StartRideCommandHandler _startHandler;
    private readonly CompleteRideCommandHandler _completeHandler;

    public DriverLifecycleCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _enRouteHandler = new DriverEnRouteCommandHandler(_rideRepository, _driverRepository);
        _arrivedHandler = new DriverArrivedCommandHandler(_rideRepository, _driverRepository);
        _onBoardHandler = new PassengerOnBoardCommandHandler(_rideRepository, _driverRepository);
        _startHandler = new StartRideCommandHandler(_rideRepository, _driverRepository);
        _completeHandler = new CompleteRideCommandHandler(_rideRepository, _driverRepository, _vehicleRepository, _fareCalculator, _dynamicPricingProvider, new FakeNotificationDispatcher());
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId, Guid VehicleId)> CreateAcceptedRideAsync()
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
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        return (customerId, rideId, driverUserId, vehicle.Id);
    }

    [Fact]
    public async Task FullHappyPath_EnRouteToComplete_Succeeds()
    {
        var (_, rideId, driverUserId, _) = await CreateAcceptedRideAsync();

        Assert.True((await _enRouteHandler.Handle(new DriverEnRouteCommand(driverUserId, rideId), CancellationToken.None)).IsSuccess);
        Assert.True((await _arrivedHandler.Handle(new DriverArrivedCommand(driverUserId, rideId), CancellationToken.None)).IsSuccess);
        Assert.True((await _onBoardHandler.Handle(new PassengerOnBoardCommand(driverUserId, rideId), CancellationToken.None)).IsSuccess);
        Assert.True((await _startHandler.Handle(new StartRideCommand(driverUserId, rideId), CancellationToken.None)).IsSuccess);

        var completeResult = await _completeHandler.Handle(new CompleteRideCommand(driverUserId, rideId, 10m, 20), CancellationToken.None);
        Assert.True(completeResult.IsSuccess);
        Assert.True(completeResult.Value > 0);

        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.AwaitingPayment, ride!.Status);
        Assert.Equal(10m, ride.ActualDistanceKm);

        var driver = await _driverRepository.GetByIdAsync((await _driverRepository.GetByUserIdAsync(driverUserId, CancellationToken.None))!.Id, CancellationToken.None);
        Assert.Equal(DriverAvailabilityStatus.Available, driver!.AvailabilityStatus);
    }

    [Fact]
    public async Task StartRide_BeforePassengerOnBoard_ReturnsConflict()
    {
        var (_, rideId, driverUserId, _) = await CreateAcceptedRideAsync();

        var result = await _startHandler.Handle(new StartRideCommand(driverUserId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CompleteRide_CalledTwice_IsIdempotent()
    {
        var (_, rideId, driverUserId, _) = await CreateAcceptedRideAsync();
        await _enRouteHandler.Handle(new DriverEnRouteCommand(driverUserId, rideId), CancellationToken.None);
        await _arrivedHandler.Handle(new DriverArrivedCommand(driverUserId, rideId), CancellationToken.None);
        await _onBoardHandler.Handle(new PassengerOnBoardCommand(driverUserId, rideId), CancellationToken.None);
        await _startHandler.Handle(new StartRideCommand(driverUserId, rideId), CancellationToken.None);

        var firstResult = await _completeHandler.Handle(new CompleteRideCommand(driverUserId, rideId, 10m, 20), CancellationToken.None);
        var secondResult = await _completeHandler.Handle(new CompleteRideCommand(driverUserId, rideId, 10m, 20), CancellationToken.None);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(firstResult.Value, secondResult.Value);
    }

    [Fact]
    public async Task EnRoute_ByUnrelatedDriver_ReturnsNotFound()
    {
        var (_, rideId, _, _) = await CreateAcceptedRideAsync();

        var result = await _enRouteHandler.Handle(new DriverEnRouteCommand(Guid.NewGuid(), rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
