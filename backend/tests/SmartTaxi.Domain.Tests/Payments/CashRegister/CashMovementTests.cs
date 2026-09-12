using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Domain.Tests.Payments.CashRegister;

public class CashMovementTests
{
    [Fact]
    public void Create_WithNonZeroAmount_Succeeds()
    {
        var movement = new CashMovement(Guid.NewGuid(), CashMovementType.RideIncome, 25m, "Course RD-1", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(25m, movement.Amount);
        Assert.Equal(CashMovementType.RideIncome, movement.MovementType);
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CashMovement(Guid.NewGuid(), CashMovementType.Other, 0m, null, Guid.NewGuid(), DateTime.UtcNow));
    }
}
