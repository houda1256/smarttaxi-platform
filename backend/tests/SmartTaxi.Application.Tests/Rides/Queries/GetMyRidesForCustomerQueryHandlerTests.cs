using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Queries.GetMyRidesForCustomer;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Queries;

public class GetMyRidesForCustomerQueryHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly CreateRideCommandHandler _createHandler;
    private readonly GetMyRidesForCustomerQueryHandler _handler;

    public GetMyRidesForCustomerQueryHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _handler = new GetMyRidesForCustomerQueryHandler(_rideRepository);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyThatCustomersRides()
    {
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        for (var i = 0; i < 2; i++)
        {
            await _createHandler.Handle(
                new CreateRideCommand(
                    customerA, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false,
                    false, false, null, RidePaymentMethod.Cash, null),
                CancellationToken.None);
        }

        await _createHandler.Handle(
            new CreateRideCommand(
                customerB, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        var result = await _handler.Handle(new GetMyRidesForCustomerQuery(customerA), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(customerA, r.CustomerId));
    }
}
