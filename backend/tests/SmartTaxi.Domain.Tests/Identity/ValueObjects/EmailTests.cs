using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Domain.Tests.Identity.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.com")]
    public void Create_WithValidFormat_ReturnsEmailWithTrimmedValue(string value)
    {
        var email = Email.Create($"  {value}  ");

        Assert.Equal(value, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespace_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => Email.Create(value!));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("no-at-sign.com")]
    public void Create_WithInvalidFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Email.Create(value));
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        var lower = Email.Create("user@example.com");
        var upper = Email.Create("USER@EXAMPLE.COM");

        Assert.Equal(lower, upper);
        Assert.True(lower == upper);
    }

    [Fact]
    public void Equality_DiffersForDifferentValues()
    {
        var first = Email.Create("first@example.com");
        var second = Email.Create("second@example.com");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void TryCreate_WithValidFormat_ReturnsTrueWithEmailAndNoError()
    {
        var succeeded = Email.TryCreate("  user@example.com  ", out var email, out var error);

        Assert.True(succeeded);
        Assert.Equal("user@example.com", email!.Value);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void TryCreate_WithEmptyOrInvalidValue_ReturnsFalseWithErrorAndNoEmail(string? value)
    {
        var succeeded = Email.TryCreate(value, out var email, out var error);

        Assert.False(succeeded);
        Assert.Null(email);
        Assert.NotNull(error);
    }

    [Fact]
    public void Create_DelegatesToTryCreate_AndThrowsWithSameMessage()
    {
        Email.TryCreate("not-an-email", out _, out var expectedError);

        var exception = Assert.Throws<ArgumentException>(() => Email.Create("not-an-email"));

        Assert.StartsWith(expectedError!, exception.Message);
    }
}
