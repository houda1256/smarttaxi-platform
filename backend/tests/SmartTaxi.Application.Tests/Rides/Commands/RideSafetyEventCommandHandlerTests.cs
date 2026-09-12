using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.AcknowledgeSos;
using SmartTaxi.Application.Rides.Commands.ActivateSos;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.ResolveSos;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class RideSafetyEventCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeRideSafetyEventRepository _safetyEventRepository = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly ActivateSosCommandHandler _activateHandler;
    private readonly AcknowledgeSosCommandHandler _acknowledgeHandler;
    private readonly ResolveSosCommandHandler _resolveHandler;

    public RideSafetyEventCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _activateHandler = new ActivateSosCommandHandler(_rideRepository, _driverRepository, _safetyEventRepository);
        _acknowledgeHandler = new AcknowledgeSosCommandHandler(_safetyEventRepository);
        _resolveHandler = new ResolveSosCommandHandler(_safetyEventRepository);
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
    public async Task ActivateSos_ByCustomer_Succeeds()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _activateHandler.Handle(
            new ActivateSosCommand(customerId, rideId, 36.81, 10.15, "Je me sens en danger"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var events = await _safetyEventRepository.GetForRideAsync(rideId, CancellationToken.None);
        Assert.Single(events);
        Assert.Equal(RideSafetyEventStatus.Open, events.Single().Status);
    }

    [Fact]
    public async Task ActivateSos_ByDriver_Succeeds()
    {
        var (_, rideId, driverUserId) = await CreateAcceptedRideAsync();

        var result = await _activateHandler.Handle(
            new ActivateSosCommand(driverUserId, rideId, 36.81, 10.15, "Passager agressif"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ActivateSos_ByNonParticipant_ReturnsForbidden()
    {
        var (_, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _activateHandler.Handle(
            new ActivateSosCommand(Guid.NewGuid(), rideId, 36.81, 10.15, "Test"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task AcknowledgeThenResolve_Sos_Succeeds()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();
        var activateResult = await _activateHandler.Handle(
            new ActivateSosCommand(customerId, rideId, 36.81, 10.15, "Danger"), CancellationToken.None);

        var acknowledgeResult = await _acknowledgeHandler.Handle(new AcknowledgeSosCommand(activateResult.Value), CancellationToken.None);
        var resolveResult = await _resolveHandler.Handle(new ResolveSosCommand(activateResult.Value), CancellationToken.None);

        Assert.True(acknowledgeResult.IsSuccess);
        Assert.True(resolveResult.IsSuccess);
    }
}
