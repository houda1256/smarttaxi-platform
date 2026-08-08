using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Domain.Tests.Payments.CashDeclarations;

public class CashDeclarationTests
{
    [Fact]
    public void Submit_ComputesDifference()
    {
        var declaration = CashDeclaration.Submit(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), 200m, 180m,
            CashDeclarationOperatingModel.DriverKeepsCashOwesShare, DateTime.UtcNow);

        Assert.Equal(-20m, declaration.Difference);
        Assert.Equal(CashDeclarationStatus.Submitted, declaration.Status);
    }

    [Fact]
    public void Submit_WithEndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => CashDeclaration.Submit(
            Guid.NewGuid(), null, new DateOnly(2026, 1, 7), new DateOnly(2026, 1, 1), 100m, 100m,
            CashDeclarationOperatingModel.DriverRemitsCashToOwner, DateTime.UtcNow));
    }

    [Fact]
    public void Submit_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => CashDeclaration.Submit(
            Guid.NewGuid(), null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), -5m, 100m,
            CashDeclarationOperatingModel.DriverRemitsCashToOwner, DateTime.UtcNow));
    }
}
