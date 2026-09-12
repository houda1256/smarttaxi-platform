using SmartTaxi.Domain.Rides;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides;

public class RideStatusTransitionsTests
{
    [Theory]
    [InlineData(RideStatus.Draft, RideStatus.Searching)]
    [InlineData(RideStatus.Searching, RideStatus.DriversAvailable)]
    [InlineData(RideStatus.DriversAvailable, RideStatus.DriverSelected)]
    [InlineData(RideStatus.DriverSelected, RideStatus.PendingDriverResponse)]
    [InlineData(RideStatus.PendingDriverResponse, RideStatus.DriverAccepted)]
    [InlineData(RideStatus.DriverAccepted, RideStatus.DriverEnRoute)]
    [InlineData(RideStatus.DriverEnRoute, RideStatus.DriverArrived)]
    [InlineData(RideStatus.DriverArrived, RideStatus.PassengerOnBoard)]
    [InlineData(RideStatus.PassengerOnBoard, RideStatus.InProgress)]
    [InlineData(RideStatus.InProgress, RideStatus.AwaitingPayment)]
    [InlineData(RideStatus.AwaitingPayment, RideStatus.Completed)]
    public void CanTransition_AlongTheHappyPath_ReturnsTrue(RideStatus from, RideStatus to)
    {
        Assert.True(RideStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RideStatus.Draft, RideStatus.InProgress)]
    [InlineData(RideStatus.Draft, RideStatus.Completed)]
    [InlineData(RideStatus.Completed, RideStatus.InProgress)]
    [InlineData(RideStatus.PassengerOnBoard, RideStatus.DriverEnRoute)]
    public void CanTransition_SkippingOrReversingSteps_ReturnsFalse(RideStatus from, RideStatus to)
    {
        Assert.False(RideStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RideStatus.Draft)]
    [InlineData(RideStatus.Searching)]
    [InlineData(RideStatus.DriverAccepted)]
    [InlineData(RideStatus.InProgress)]
    public void CanTransition_ToCancelledByAdmin_IsAlwaysAllowedFromNonTerminalStatuses(RideStatus from)
    {
        Assert.True(RideStatusTransitions.CanTransition(from, RideStatus.CancelledByAdmin));
    }

    [Theory]
    [InlineData(RideStatus.Completed)]
    [InlineData(RideStatus.CancelledByCustomer)]
    [InlineData(RideStatus.Expired)]
    public void CanTransition_ToCancelledByAdmin_FromTerminalStatuses_ReturnsFalse(RideStatus from)
    {
        Assert.False(RideStatusTransitions.CanTransition(from, RideStatus.CancelledByAdmin));
    }

    [Fact]
    public void IsTerminal_ForCompletedAndCancelledStatuses_ReturnsTrue()
    {
        Assert.True(RideStatusTransitions.IsTerminal(RideStatus.Completed));
        Assert.True(RideStatusTransitions.IsTerminal(RideStatus.CancelledByCustomer));
        Assert.False(RideStatusTransitions.IsTerminal(RideStatus.InProgress));
    }
}
