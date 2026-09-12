using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Domain.Tests.Payments.Payouts;

public class PayoutTests
{
    [Fact]
    public void Request_WithValidData_SetsRequestedStatus()
    {
        var payout = Payout.Request(
            Guid.NewGuid(), FinancialAccountType.Driver, 100m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.Weekly,
            DateTime.UtcNow);

        Assert.Equal(PayoutStatus.Requested, payout.Status);
        Assert.Equal(100m, payout.Amount);
    }

    [Fact]
    public void Request_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payout.Request(
            Guid.NewGuid(), FinancialAccountType.Driver, 0m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.Weekly, DateTime.UtcNow));
    }

    [Fact]
    public void Request_ForPlatformAccount_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payout.Request(
            Guid.NewGuid(), FinancialAccountType.Platform, 100m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.Weekly, DateTime.UtcNow));
    }
}
