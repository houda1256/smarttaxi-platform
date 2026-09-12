using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class CreateRideCommandHandlerTests
{
    private readonly FakeRideRepository _repository = new();
    private readonly CreateRideCommandHandler _handler;

    public CreateRideCommandHandlerTests()
    {
        _handler = new CreateRideCommandHandler(_repository);
    }

    private static CreateRideCommand ImmediateCommand(DateTime? scheduledAt = null, RideType rideType = RideType.Immediate) => new(
        Guid.NewGuid(), rideType, "Pickup St", 36.8065, 10.1815, "Dest St", 36.85, 10.2, scheduledAt, 1, 0, false,
        false, false, false, null, RidePaymentMethod.Cash, null);

    [Fact]
    public async Task Handle_Immediate_CreatesRideInDraftStatus()
    {
        var result = await _handler.Handle(ImmediateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ride = await _repository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(RideStatus.Draft, ride!.Status);
    }

    [Fact]
    public async Task Handle_Scheduled_InTheFuture_Succeeds()
    {
        var command = ImmediateCommand(DateTime.UtcNow.AddHours(3), RideType.Scheduled);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Scheduled_InThePast_ReturnsValidationError()
    {
        var command = ImmediateCommand(DateTime.UtcNow.AddMinutes(-10), RideType.Scheduled);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithInvalidLatitude_ReturnsValidationError()
    {
        var command = new CreateRideCommand(
            Guid.NewGuid(), RideType.Immediate, "Pickup St", 999, 10.1815, "Dest St", 36.85, 10.2, null, 1, 0,
            false, false, false, false, null, RidePaymentMethod.Cash, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
