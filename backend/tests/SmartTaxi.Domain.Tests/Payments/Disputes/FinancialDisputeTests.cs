using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Domain.Tests.Payments.Disputes;

public class FinancialDisputeTests
{
    [Fact]
    public void Open_WithRelatedPayment_Succeeds()
    {
        var dispute = FinancialDispute.Open(
            FinancialDisputeCategory.IncorrectFare, Guid.NewGuid(), null, null, 25m, "TND", "Tarif incorrect", null,
            Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(FinancialDisputeStatus.Open, dispute.Status);
        Assert.Equal(25m, dispute.DisputedAmount);
    }

    [Fact]
    public void Open_WithNoRelatedEntity_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialDispute.Open(
            FinancialDisputeCategory.Other, null, null, null, 25m, "TND", "Description", null, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Open_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialDispute.Open(
            FinancialDisputeCategory.Other, Guid.NewGuid(), null, null, 0m, "TND", "Description", null, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Open_WithBlankDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() => FinancialDispute.Open(
            FinancialDisputeCategory.Other, Guid.NewGuid(), null, null, 10m, "TND", " ", null, Guid.NewGuid(), DateTime.UtcNow));
    }
}
