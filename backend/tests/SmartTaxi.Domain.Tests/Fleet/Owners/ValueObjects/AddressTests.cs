using SmartTaxi.Domain.Fleet.Owners.ValueObjects;

namespace SmartTaxi.Domain.Tests.Fleet.Owners.ValueObjects;

public class AddressTests
{
    [Fact]
    public void TryCreate_WithMissingCity_ReturnsFalse()
    {
        var result = Address.TryCreate("Street", "", "20000", "Maroc", out var address, out var error);

        Assert.False(result);
        Assert.Null(address);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_WithValidFields_Succeeds()
    {
        var result = Address.TryCreate("Street", "City", null, "Country", out var address, out var error);

        Assert.True(result);
        Assert.NotNull(address);
        Assert.Null(error);
    }
}
