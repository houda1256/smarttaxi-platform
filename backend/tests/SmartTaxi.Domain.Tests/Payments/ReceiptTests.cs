using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Tests.Payments;

public class ReceiptTests
{
    [Fact]
    public void Issue_WithValidData_RaisesEvent()
    {
        var receipt = Receipt.Issue(Guid.NewGuid(), 22m, "TND", DateTime.UtcNow);

        Assert.StartsWith("REC-", receipt.ReceiptNumber);
        Assert.Equal(22m, receipt.Amount);
        Assert.Single(receipt.DomainEvents);
        Assert.IsType<ReceiptGenerated>(receipt.DomainEvents.Single());
    }

    [Fact]
    public void Issue_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => Receipt.Issue(Guid.NewGuid(), -1m, "TND", DateTime.UtcNow));
    }
}
