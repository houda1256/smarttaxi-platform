using SmartTaxi.Infrastructure.Identity.Services;

namespace SmartTaxi.Infrastructure.Tests.Identity.Services;

public class RefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_ProducesDifferentValuesEachTime()
    {
        var generator = new RefreshTokenGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_ProducesHighEntropyValue()
    {
        var generator = new RefreshTokenGenerator();

        var token = generator.Generate();

        // 256 bits, Base64Url-encoded, is 43 characters (no padding).
        Assert.True(token.Length >= 40, $"Expected a high-entropy token, got length {token.Length}.");
    }

    [Fact]
    public void Generate_IsUrlSafe()
    {
        var generator = new RefreshTokenGenerator();

        var token = generator.Generate();

        Assert.DoesNotContain('=', token);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
    }
}
