using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Tests.Payments;

public class RefundRecordTests
{
    [Fact]
    public void Issue_WithValidData_RaisesEvent()
    {
        var record = RefundRecord.Issue(Guid.NewGuid(), 10m, "TND", RefundType.Partial, "Client insatisfait", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(10m, record.Amount);
        Assert.Equal(RefundType.Partial, record.RefundType);
        Assert.Single(record.DomainEvents);
        Assert.IsType<RefundIssued>(record.DomainEvents.Single());
    }

    [Fact]
    public void Issue_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => RefundRecord.Issue(Guid.NewGuid(), 0m, "TND", RefundType.Full, "Test", Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Issue_WithBlankReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => RefundRecord.Issue(Guid.NewGuid(), 10m, "TND", RefundType.Full, " ", Guid.NewGuid(), DateTime.UtcNow));
    }
}
