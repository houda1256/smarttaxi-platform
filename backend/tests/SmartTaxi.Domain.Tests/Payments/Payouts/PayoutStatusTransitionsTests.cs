using SmartTaxi.Domain.Payments.Payouts;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Domain.Tests.Payments.Payouts;

public class PayoutStatusTransitionsTests
{
    [Theory]
    [InlineData(PayoutStatus.Requested, PayoutStatus.PendingApproval)]
    [InlineData(PayoutStatus.PendingApproval, PayoutStatus.Approved)]
    [InlineData(PayoutStatus.Approved, PayoutStatus.Processing)]
    [InlineData(PayoutStatus.Processing, PayoutStatus.Paid)]
    [InlineData(PayoutStatus.Processing, PayoutStatus.Failed)]
    public void CanTransition_AlongAllowedPaths_ReturnsTrue(PayoutStatus from, PayoutStatus to)
    {
        Assert.True(PayoutStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    [InlineData(PayoutStatus.Requested, PayoutStatus.Paid)]
    [InlineData(PayoutStatus.Paid, PayoutStatus.Processing)]
    [InlineData(PayoutStatus.Rejected, PayoutStatus.Approved)]
    public void CanTransition_IllegalPaths_ReturnsFalse(PayoutStatus from, PayoutStatus to)
    {
        Assert.False(PayoutStatusTransitions.CanTransition(from, to));
    }

    [Fact]
    public void IsTerminal_ForPaidFailedRejectedCancelled_ReturnsTrue()
    {
        Assert.True(PayoutStatusTransitions.IsTerminal(PayoutStatus.Paid));
        Assert.True(PayoutStatusTransitions.IsTerminal(PayoutStatus.Failed));
        Assert.True(PayoutStatusTransitions.IsTerminal(PayoutStatus.Rejected));
        Assert.True(PayoutStatusTransitions.IsTerminal(PayoutStatus.Cancelled));
        Assert.False(PayoutStatusTransitions.IsTerminal(PayoutStatus.Processing));
    }
}
