using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CancelRideByAdmin;
using SmartTaxi.Application.Rides.Commands.CancelRideByCustomer;
using SmartTaxi.Application.Rides.Commands.CancelRideByDriver;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class CancelRideCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeFarePricingPolicy _pricingPolicy = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly CancelRideByCustomerCommandHandler _cancelByCustomerHandler;
    private readonly CancelRideByDriverCommandHandler _cancelByDriverHandler;
    private readonly CancelRideByAdminCommandHandler _cancelByAdminHandler;

    public CancelRideCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _cancelByCustomerHandler = new CancelRideByCustomerCommandHandler(_rideRepository, _driverRepository, _holdRepository, _pricingPolicy);
        _cancelByDriverHandler = new CancelRideByDriverCommandHandler(_rideRepository, _driverRepository);
        _cancelByAdminHandler = new CancelRideByAdminCommandHandler(_rideRepository, _holdRepository, _driverRepository);
    }

    private async Task<Guid> CreateRideInDraftAsync(Guid? customerId = null)
    {
        var result = await _createHandler.Handle(
            new CreateRideCommand(
                customerId ?? Guid.NewGuid(), RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0,
                false, false, false, false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        return result.Value;
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId)> CreateAcceptedRideAsync()
    {
        var customerId = Guid.NewGuid();
        var rideId = await CreateRideInDraftAsync(customerId);

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
    public async Task CustomerCancel_BeforeDriverAcceptance_IsFree()
    {
        var customerId = Guid.NewGuid();
        var rideId = await CreateRideInDraftAsync(customerId);

        var result = await _cancelByCustomerHandler.Handle(
            new CancelRideByCustomerCommand(customerId, rideId, CustomerCancellationReason.ChangedMind, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.CancelledByCustomer, ride!.Status);
    }

    [Fact]
    public async Task CustomerCancel_AfterDriverAcceptance_ChargesAcceptanceFee()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _cancelByCustomerHandler.Handle(
            new CancelRideByCustomerCommand(customerId, rideId, CustomerCancellationReason.ChangedMind, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_pricingPolicy.CancellationFeeAfterAcceptance, result.Value);
    }

    [Fact]
    public async Task CustomerCancel_AfterRideStarted_ReturnsConflict()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverAccepted, RideStatus.DriverEnRoute, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryMarkDriverArrivedAsync(rideId, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.DriverArrived, RideStatus.PassengerOnBoard, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.PassengerOnBoard, RideStatus.InProgress, null, null, DateTime.UtcNow, CancellationToken.None);

        var result = await _cancelByCustomerHandler.Handle(
            new CancelRideByCustomerCommand(customerId, rideId, CustomerCancellationReason.ChangedMind, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task DriverCancel_AfterAcceptance_ReleasesDriverAndCancelsRide()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();

        var result = await _cancelByDriverHandler.Handle(
            new CancelRideByDriverCommand(driverUserId, rideId, DriverCancellationReason.VehicleProblem, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.CancelledByDriver, ride!.Status);
    }

    [Fact]
    public async Task AdminCancel_FromAnyNonTerminalStatus_Succeeds()
    {
        var customerId = Guid.NewGuid();
        var rideId = await CreateRideInDraftAsync(customerId);

        var result = await _cancelByAdminHandler.Handle(new CancelRideByAdminCommand(Guid.NewGuid(), rideId, "Fraude suspectée"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.CancelledByAdmin, ride!.Status);
    }

    [Fact]
    public async Task AdminCancel_OnAlreadyTerminalRide_ReturnsConflict()
    {
        var customerId = Guid.NewGuid();
        var rideId = await CreateRideInDraftAsync(customerId);
        await _cancelByCustomerHandler.Handle(
            new CancelRideByCustomerCommand(customerId, rideId, CustomerCancellationReason.ChangedMind, null), CancellationToken.None);

        var result = await _cancelByAdminHandler.Handle(new CancelRideByAdminCommand(Guid.NewGuid(), rideId, "test"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
