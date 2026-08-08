using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.SearchDrivers;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class SelectDriverCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _handler;

    public SelectDriverCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _handler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverId, Guid VehicleId)> CreateRideWithRecommendationAsync()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        await _rideRepository.TryTransitionAsync(createResult.Value, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(createResult.Value, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);

        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0, FuelType.Petrol,
            TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await _recommendationRepository.ReplaceForRideAsync(
            createResult.Value,
            [new Domain.Rides.Entities.RideDriverRecommendation(createResult.Value, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        return (customerId, createResult.Value, driver.Id, vehicle.Id);
    }

    [Fact]
    public async Task Handle_ForRecommendedDriver_CreatesHoldAndMovesRideToPendingDriverResponse()
    {
        var (customerId, rideId, driverId, vehicleId) = await CreateRideWithRecommendationAsync();

        var result = await _handler.Handle(new SelectDriverCommand(customerId, rideId, driverId, vehicleId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.PendingDriverResponse, ride!.Status);
        Assert.Equal(driverId, ride.SelectedDriverId);
    }

    [Fact]
    public async Task Handle_ForDriverNotInRecommendationList_ReturnsValidationError()
    {
        var (customerId, rideId, _, _) = await CreateRideWithRecommendationAsync();

        var result = await _handler.Handle(
            new SelectDriverCommand(customerId, rideId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_TwoCustomersSelectingSameDriverConcurrently_OnlyOneSucceeds()
    {
        var (customerA, rideIdA, driverId, vehicleId) = await CreateRideWithRecommendationAsync();

        // A second, independent Ride recommends the very same Driver/Vehicle pair.
        var customerB = Guid.NewGuid();
        var createResultB = await _createHandler.Handle(
            new CreateRideCommand(
                customerB, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        await _rideRepository.TryTransitionAsync(createResultB.Value, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(createResultB.Value, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);
        await _recommendationRepository.ReplaceForRideAsync(
            createResultB.Value,
            [new Domain.Rides.Entities.RideDriverRecommendation(createResultB.Value, driverId, vehicleId, 3.0m, 6, 35m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        var resultA = await _handler.Handle(new SelectDriverCommand(customerA, rideIdA, driverId, vehicleId), CancellationToken.None);
        var resultB = await _handler.Handle(new SelectDriverCommand(customerB, createResultB.Value, driverId, vehicleId), CancellationToken.None);

        Assert.True(resultA.IsSuccess);
        Assert.False(resultB.IsSuccess);
        Assert.Equal(ErrorType.Conflict, resultB.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenRideNotInDriversAvailable_ReturnsConflict()
    {
        var (customerId, rideId, driverId, vehicleId) = await CreateRideWithRecommendationAsync();
        await _handler.Handle(new SelectDriverCommand(customerId, rideId, driverId, vehicleId), CancellationToken.None);

        var result = await _handler.Handle(new SelectDriverCommand(customerId, rideId, driverId, vehicleId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
