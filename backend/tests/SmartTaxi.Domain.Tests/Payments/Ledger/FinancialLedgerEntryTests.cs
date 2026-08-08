using SmartTaxi.Domain.Payments.Ledger.Entities;
using SmartTaxi.Domain.Payments.Ledger.Enums;

namespace SmartTaxi.Domain.Tests.Payments.Ledger;

public class FinancialLedgerEntryTests
{
    [Fact]
    public void Post_WithValidData_GeneratesTransactionNumber()
    {
        var entry = FinancialLedgerEntry.Post(
            Guid.NewGuid(), Guid.NewGuid(), 100m, "TND", LedgerEntryType.PaymentCollected, "Payment", Guid.NewGuid(),
            "Course terminée", null, DateTime.UtcNow);

        Assert.StartsWith("TXN-", entry.TransactionNumber);
        Assert.Equal(100m, entry.Amount);
        Assert.Null(entry.ReversalOfEntryId);
    }

    [Fact]
    public void Post_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialLedgerEntry.Post(
            Guid.NewGuid(), Guid.NewGuid(), 0m, "TND", LedgerEntryType.Fee, "Payment", Guid.NewGuid(), null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Post_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialLedgerEntry.Post(
            Guid.NewGuid(), Guid.NewGuid(), -5m, "TND", LedgerEntryType.Fee, "Payment", Guid.NewGuid(), null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Post_WithBlankSourceType_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialLedgerEntry.Post(
            Guid.NewGuid(), Guid.NewGuid(), 10m, "TND", LedgerEntryType.Fee, " ", Guid.NewGuid(), null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Post_AsReversal_SetsReversalOfEntryId()
    {
        var originalId = Guid.NewGuid();

        var reversal = FinancialLedgerEntry.Post(
            Guid.NewGuid(), Guid.NewGuid(), 20m, "TND", LedgerEntryType.Reversal, "Refund", Guid.NewGuid(), "Annulation",
            null, DateTime.UtcNow, originalId);

        Assert.Equal(originalId, reversal.ReversalOfEntryId);
    }

    [Fact]
    public void Post_SameAccountOnBothSides_IsAllowed()
    {
        var accountId = Guid.NewGuid();

        var entry = FinancialLedgerEntry.Post(
            accountId, accountId, 15m, "TND", LedgerEntryType.PlatformCommission, "Payment", Guid.NewGuid(), null, null,
            DateTime.UtcNow);

        Assert.Equal(accountId, entry.DebitAccountId);
        Assert.Equal(accountId, entry.CreditAccountId);
    }
}
