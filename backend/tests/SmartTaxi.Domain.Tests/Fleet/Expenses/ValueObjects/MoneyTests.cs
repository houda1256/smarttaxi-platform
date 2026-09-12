using SmartTaxi.Domain.Fleet.Expenses.ValueObjects;

namespace SmartTaxi.Domain.Tests.Fleet.Expenses.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void TryCreate_WithNegativeAmount_ReturnsFalse()
    {
        var result = Money.TryCreate(-1m, "MAD", out var money, out var error);

        Assert.False(result);
        Assert.Null(money);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_WithInvalidCurrencyLength_ReturnsFalse()
    {
        var result = Money.TryCreate(10m, "M", out var money, out var error);

        Assert.False(result);
        Assert.Null(money);
    }

    [Fact]
    public void TryCreate_WithValidValues_NormalizesCurrencyToUppercase()
    {
        var result = Money.TryCreate(10m, "mad", out var money, out var error);

        Assert.True(result);
        Assert.Equal("MAD", money!.Currency);
    }
}
