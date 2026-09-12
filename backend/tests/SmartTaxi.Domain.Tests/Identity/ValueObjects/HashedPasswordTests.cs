using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Domain.Tests.Identity.ValueObjects;

public class HashedPasswordTests
{
    [Fact]
    public void Create_WithNonEmptyValue_ReturnsHashedPassword()
    {
        var hashedPassword = HashedPassword.Create("some-hashed-value");

        Assert.Equal("some-hashed-value", hashedPassword.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespace_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => HashedPassword.Create(value!));
    }

    [Fact]
    public void Equality_IsBasedOnValue()
    {
        var first = HashedPassword.Create("same-hash");
        var second = HashedPassword.Create("same-hash");
        var third = HashedPassword.Create("different-hash");

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }
}
