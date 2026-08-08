using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.ApproveSharedRideByDriver;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.CustomerApproveSharedRide;
using SmartTaxi.Application.Rides.Commands.CustomerRejectSharedRide;
using SmartTaxi.Application.Rides.Commands.FindSharedRideMatch;
using SmartTaxi.Application.Rides.Commands.RejectSharedRideByDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class SharedRideCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeSharedRideMatchRepository _matchRepository = new();
    private readonly FakeSharedRideParticipantRepository _participantRepository = new();
    private readonly FakeSharedRideMatchingService _matchingService = new();
    private readonly FakeSharedRideMatchingPolicy _matchingPolicy = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly FindSharedRideMatchCommandHandler _findMatchHandler;
    private readonly CustomerApproveSharedRideCommandHandler _customerApproveHandler;
    private readonly CustomerRejectSharedRideCommandHandler _customerRejectHandler;
    private readonly ApproveSharedRideByDriverCommandHandler _driverApproveHandler;
    private readonly RejectSharedRideByDriverCommandHandler _driverRejectHandler;

    public SharedRideCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _findMatchHandler = new FindSharedRideMatchCommandHandler(
            _rideRepository, _matchRepository, _participantRepository, _matchingService, _matchingPolicy);
        _customerApproveHandler = new CustomerApproveSharedRideCommandHandler(_matchRepository, _participantRepository);
        _customerRejectHandler = new CustomerRejectSharedRideCommandHandler(_matchRepository, _participantRepository);
        _driverApproveHandler = new ApproveSharedRideByDriverCommandHandler(
            _matchRepository, _participantRepository, _rideRepository, _driverRepository, _vehicleRepository);
        _driverRejectHandler = new RejectSharedRideByDriverCommandHandler(_matchRepository, _driverRepository);
    }

    private async Task<Guid> CreateSharedRideAsync(Guid customerId, int passengerCount = 1)
    {
        var result = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Shared, "A", 36.8, 10.1, "B", 36.9, 10.2, null, passengerCount, 0, false, false,
                false, false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        await _rideRepository.TryTransitionAsync(result.Value, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        return result.Value;
    }

    private async Task<(Guid CustomerA, Guid RideA, Guid CustomerB, Guid RideB, Guid MatchId)> CreateWaitingForCustomerApprovalsMatchAsync()
    {
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();
        var rideA = await CreateSharedRideAsync(customerA);
        var rideB = await CreateSharedRideAsync(customerB);

        var findResult = await _findMatchHandler.Handle(new FindSharedRideMatchCommand(customerA, rideA), CancellationToken.None);

        Assert.True(findResult.IsSuccess);
        Assert.NotNull(findResult.Value);

        return (customerA, rideA, customerB, rideB, findResult.Value!.Value);
    }

    [Fact]
    public async Task FindMatch_ForCompatibleSharedRides_CreatesMatchWithTwoParticipants()
    {
        var (_, _, _, _, matchId) = await CreateWaitingForCustomerApprovalsMatchAsync();

        var match = await _matchRepository.GetByIdAsync(matchId, CancellationToken.None);
        var participants = await _participantRepository.GetForMatchAsync(matchId, CancellationToken.None);

        Assert.Equal(SharedRideMatchStatus.WaitingForCustomerApprovals, match!.Status);
        Assert.Equal(2, participants.Count);
    }

    [Fact]
    public async Task FindMatch_WhenIncompatible_ReturnsNullMatch()
    {
        _matchingService.IsCompatible = false;
        var customerA = Guid.NewGuid();
        var rideA = await CreateSharedRideAsync(customerA);
        await CreateSharedRideAsync(Guid.NewGuid());

        var result = await _findMatchHandler.Handle(new FindSharedRideMatchCommand(customerA, rideA), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task FullApprovalFlow_BothCustomersAndDriver_ConfirmsMatchAndBothRides()
    {
        var (customerA, rideA, customerB, rideB, matchId) = await CreateWaitingForCustomerApprovalsMatchAsync();

        await _customerApproveHandler.Handle(new CustomerApproveSharedRideCommand(customerA, matchId), CancellationToken.None);
        var afterFirstApproval = await _matchRepository.GetByIdAsync(matchId, CancellationToken.None);
        Assert.Equal(SharedRideMatchStatus.WaitingForCustomerApprovals, afterFirstApproval!.Status);

        await _customerApproveHandler.Handle(new CustomerApproveSharedRideCommand(customerB, matchId), CancellationToken.None);
        var afterBothApprovals = await _matchRepository.GetByIdAsync(matchId, CancellationToken.None);
        Assert.Equal(SharedRideMatchStatus.WaitingForDriverApproval, afterBothApprovals!.Status);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "HiAce", 2022, "White", "AA-123-BB", null, 0, FuelType.Diesel,
            TransmissionType.Manual, 8, true, false, VehicleCategory.Van, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var driverResult = await _driverApproveHandler.Handle(
            new ApproveSharedRideByDriverCommand(driverUserId, matchId, vehicle.Id), CancellationToken.None);

        Assert.True(driverResult.IsSuccess);
        var confirmedMatch = await _matchRepository.GetByIdAsync(matchId, CancellationToken.None);
        Assert.Equal(SharedRideMatchStatus.Confirmed, confirmedMatch!.Status);

        var reloadedRideA = await _rideRepository.GetByIdAsync(rideA, CancellationToken.None);
        var reloadedRideB = await _rideRepository.GetByIdAsync(rideB, CancellationToken.None);
        Assert.Equal(RideStatus.DriverAccepted, reloadedRideA!.Status);
        Assert.Equal(RideStatus.DriverAccepted, reloadedRideB!.Status);
        Assert.Equal(driver.Id, reloadedRideA.SelectedDriverId);
        Assert.Equal(driver.Id, reloadedRideB.SelectedDriverId);
    }

    [Fact]
    public async Task DriverApproval_WithInsufficientSeatCapacity_ReturnsValidationError()
    {
        var (customerA, _, customerB, _, matchId) = await CreateWaitingForCustomerApprovalsMatchAsync();
        await _customerApproveHandler.Handle(new CustomerApproveSharedRideCommand(customerA, matchId), CancellationToken.None);
        await _customerApproveHandler.Handle(new CustomerApproveSharedRideCommand(customerB, matchId), CancellationToken.None);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        var tinyVehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Fiat", "500", 2022, "Red", "BB-999-CC", null, 0, FuelType.Petrol,
            TransmissionType.Manual, 1, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(tinyVehicle, CancellationToken.None);

        var result = await _driverApproveHandler.Handle(
            new ApproveSharedRideByDriverCommand(driverUserId, matchId, tinyVehicle.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CustomerReject_LeavesUnderlyingRidesUntouched()
    {
        var (customerA, rideA, _, rideB, matchId) = await CreateWaitingForCustomerApprovalsMatchAsync();

        var result = await _customerRejectHandler.Handle(new CustomerRejectSharedRideCommand(customerA, matchId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var match = await _matchRepository.GetByIdAsync(matchId, CancellationToken.None);
        Assert.Equal(SharedRideMatchStatus.Rejected, match!.Status);

        var reloadedRideA = await _rideRepository.GetByIdAsync(rideA, CancellationToken.None);
        var reloadedRideB = await _rideRepository.GetByIdAsync(rideB, CancellationToken.None);
        Assert.Equal(RideStatus.Searching, reloadedRideA!.Status);
        Assert.Equal(RideStatus.Searching, reloadedRideB!.Status);
    }

    [Fact]
    public async Task DriverReject_LeavesUnderlyingRidesUntouched()
    {
        var (customerA, rideA, customerB, rideB, matchId) = await CreateWaitingForCustomerApprovalsMatchAsync();
        await _customerApproveHandler.Handle(new CustomerApproveSharedRideCommand(customerA, matchId), CancellationToken.None);
        await _customerApproveHandler.Handle(new CustomerApproveSharedRideCommand(customerB, matchId), CancellationToken.None);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);

        var result = await _driverRejectHandler.Handle(new RejectSharedRideByDriverCommand(driverUserId, matchId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRideA = await _rideRepository.GetByIdAsync(rideA, CancellationToken.None);
        var reloadedRideB = await _rideRepository.GetByIdAsync(rideB, CancellationToken.None);
        Assert.Equal(RideStatus.Searching, reloadedRideA!.Status);
        Assert.Equal(RideStatus.Searching, reloadedRideB!.Status);
    }
}
