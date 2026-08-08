using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;

namespace SmartTaxi.Domain.Tests.Payments.Accounts;

public class FinancialAccountTests
{
    [Fact]
    public void Open_PlatformAccount_WithNoOwnerReference_Succeeds()
    {
        var account = FinancialAccount.Open(FinancialAccountType.Platform, null, "TND", DateTime.UtcNow);

        Assert.Equal(FinancialAccountType.Platform, account.AccountType);
        Assert.Null(account.OwnerReferenceId);
        Assert.Equal(0m, account.PendingBalance);
    }

    [Fact]
    public void Open_PlatformAccount_WithOwnerReference_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialAccount.Open(FinancialAccountType.Platform, Guid.NewGuid(), "TND", DateTime.UtcNow));
    }

    [Fact]
    public void Open_DriverAccount_WithoutOwnerReference_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialAccount.Open(FinancialAccountType.Driver, null, "TND", DateTime.UtcNow));
    }

    [Fact]
    public void Open_DriverAccount_WithOwnerReference_Succeeds()
    {
        var driverId = Guid.NewGuid();
        var account = FinancialAccount.Open(FinancialAccountType.Driver, driverId, "TND", DateTime.UtcNow);

        Assert.Equal(driverId, account.OwnerReferenceId);
    }

    [Fact]
    public void Open_WithInvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialAccount.Open(FinancialAccountType.Platform, null, "TOOLONG", DateTime.UtcNow));
    }
}
