using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Drivers;
using SmartTaxi.Application.Fleet.Vehicles;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.SearchDrivers;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class SearchDriversCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverRecommendationService _recommendationService = new();
    private readonly CreateRideCommandHandler _createHandler;
    private readonly SearchDriversCommandHandler _handler;

    public SearchDriversCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _handler = new SearchDriversCommandHandler(_rideRepository, _recommendationRepository, _recommendationService);
    }

    private async Task<(Guid CustomerId, Guid RideId)> CreateDraftRideAsync()
    {
        var customerId = Guid.NewGuid();
        var result = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "Pickup St", 36.8065, 10.1815, "Dest St", 36.85, 10.2, null, 1, 0,
                false, false, false, false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        return (customerId, result.Value);
    }

    private static DriverRecommendationResult MakeRecommendation()
    {
        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0, FuelType.Petrol,
            TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        return new DriverRecommendationResult(
            driver.Id, vehicle.Id, DriverProfileSummary.FromEntity(driver), VehicleSummary.FromEntity(vehicle),
            2.5m, 5, 42.5m, ["Très proche"]);
    }

    [Fact]
    public async Task Handle_WithAvailableDrivers_MovesRideToDriversAvailable()
    {
        var (customerId, rideId) = await CreateDraftRideAsync();
        _recommendationService.Results = [MakeRecommendation()];

        var result = await _handler.Handle(new SearchDriversCommand(customerId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.DriversAvailable, ride!.Status);
        var recommendations = await _recommendationRepository.GetForRideAsync(rideId, CancellationToken.None);
        Assert.Single(recommendations);
    }

    [Fact]
    public async Task Handle_WithNoAvailableDrivers_MovesRideToNoDriverAvailable()
    {
        var (customerId, rideId) = await CreateDraftRideAsync();
        _recommendationService.Results = [];

        var result = await _handler.Handle(new SearchDriversCommand(customerId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.NoDriverAvailable, ride!.Status);
    }

    [Fact]
    public async Task Handle_ByNonOwningCustomer_ReturnsNotFound()
    {
        var (_, rideId) = await CreateDraftRideAsync();

        var result = await _handler.Handle(new SearchDriversCommand(Guid.NewGuid(), rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenNotInDraftStatus_ReturnsConflict()
    {
        var (customerId, rideId) = await CreateDraftRideAsync();
        await _handler.Handle(new SearchDriversCommand(customerId, rideId), CancellationToken.None);

        var result = await _handler.Handle(new SearchDriversCommand(customerId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
