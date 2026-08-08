using SmartTaxi.Domain.Payments;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Domain.Tests.Payments;

public class PaymentStatusTransitionsTests
{
    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Paid, PaymentStatus.PartiallyRefunded)]
    [InlineData(PaymentStatus.Paid, PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.PartiallyRefunded, PaymentStatus.PartiallyRefunded)]
    [InlineData(PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded)]
    public void CanTransition_AlongAllowedPaths_ReturnsTrue(PaymentStatus from, PaymentStatus to)
    {
        Assert.True(PaymentStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Paid, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Cancelled, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded)]
    public void CanTransition_IllegalPaths_ReturnsFalse(PaymentStatus from, PaymentStatus to)
    {
        Assert.False(PaymentStatusTransitions.CanTransition(from, to));
    }

    [Fact]
    public void IsTerminal_ForFailedCancelledRefunded_ReturnsTrue()
    {
        Assert.True(PaymentStatusTransitions.IsTerminal(PaymentStatus.Failed));
        Assert.True(PaymentStatusTransitions.IsTerminal(PaymentStatus.Cancelled));
        Assert.True(PaymentStatusTransitions.IsTerminal(PaymentStatus.Refunded));
        Assert.False(PaymentStatusTransitions.IsTerminal(PaymentStatus.Paid));
        Assert.False(PaymentStatusTransitions.IsTerminal(PaymentStatus.PartiallyRefunded));
    }
}
