using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Domain.Tests.Identity.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+21612345678")]
    [InlineData("+14155552671")]
    public void Create_WithValidE164Format_Succeeds(string value)
    {
        var phoneNumber = PhoneNumber.Create(value);

        Assert.Equal(value, phoneNumber.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespace_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(value!));
    }

    [Theory]
    [InlineData("0612345678")]
    [InlineData("+0612345678")]
    [InlineData("not-a-phone-number")]
    [InlineData("+123")]
    public void Create_WithInvalidFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(value));
    }

    [Fact]
    public void TryCreate_WithValidFormat_ReturnsTrue()
    {
        var succeeded = PhoneNumber.TryCreate("+21612345678", out var phoneNumber, out var error);

        Assert.True(succeeded);
        Assert.Equal("+21612345678", phoneNumber!.Value);
        Assert.Null(error);
    }

    [Fact]
    public void Equality_IsBasedOnValue()
    {
        var first = PhoneNumber.Create("+21612345678");
        var second = PhoneNumber.Create("+21612345678");
        var third = PhoneNumber.Create("+14155552671");

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }
}
