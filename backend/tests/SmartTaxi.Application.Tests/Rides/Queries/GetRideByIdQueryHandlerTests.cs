using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Queries.GetRideById;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Queries;

public class GetRideByIdQueryHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly CreateRideCommandHandler _createHandler;
    private readonly GetRideByIdQueryHandler _handler;

    public GetRideByIdQueryHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _handler = new GetRideByIdQueryHandler(_rideRepository, _driverRepository);
    }

    private async Task<Guid> CreateRideAsync(Guid customerId)
    {
        var result = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "Pickup St", 36.8065, 10.1815, "Dest St", 36.85, 10.2, null, 1, 0,
                false, false, false, false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        return result.Value;
    }

    [Fact]
    public async Task Handle_ByOwningCustomer_ReturnsRide()
    {
        var customerId = Guid.NewGuid();
        var rideId = await CreateRideAsync(customerId);

        var result = await _handler.Handle(new GetRideByIdQuery(customerId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ByUnrelatedUser_ReturnsNotFound()
    {
        var rideId = await CreateRideAsync(Guid.NewGuid());

        var result = await _handler.Handle(new GetRideByIdQuery(Guid.NewGuid(), rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_BySelectedDriver_ReturnsRide()
    {
        var rideId = await CreateRideAsync(Guid.NewGuid());
        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);

        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        typeof(Domain.Rides.Entities.Ride).GetProperty(nameof(Domain.Rides.Entities.Ride.SelectedDriverId))!.SetValue(ride, driver.Id);

        var result = await _handler.Handle(new GetRideByIdQuery(driverUserId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
