using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Domain.Tests.Payments.CashRegister;

public class CashRegisterSessionTests
{
    [Fact]
    public void Open_WithValidOpeningBalance_SetsOpenStatus()
    {
        var session = CashRegisterSession.Open(Guid.NewGuid(), Guid.NewGuid(), 50m, DateTime.UtcNow);

        Assert.Equal(CashRegisterSessionStatus.Open, session.Status);
        Assert.Equal(50m, session.OpeningBalance);
        Assert.Null(session.ClosedAt);
    }

    [Fact]
    public void Open_WithNegativeOpeningBalance_Throws()
    {
        Assert.Throws<ArgumentException>(() => CashRegisterSession.Open(Guid.NewGuid(), Guid.NewGuid(), -1m, DateTime.UtcNow));
    }
}
