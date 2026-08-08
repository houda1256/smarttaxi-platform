using SmartTaxi.Infrastructure.Identity.Services;

namespace SmartTaxi.Infrastructure.Tests.Identity.Services;

public class RefreshTokenHasherTests
{
    [Fact]
    public void Hash_IsDeterministic()
    {
        var hasher = new RefreshTokenHasher();

        Assert.Equal(hasher.Hash("same-raw-token"), hasher.Hash("same-raw-token"));
    }

    [Fact]
    public void Hash_DiffersForDifferentInputs()
    {
        var hasher = new RefreshTokenHasher();

        Assert.NotEqual(hasher.Hash("token-a"), hasher.Hash("token-b"));
    }

    [Fact]
    public void Hash_NeverEqualsItsOwnRawInput()
    {
        var hasher = new RefreshTokenHasher();
        const string raw = "some-raw-refresh-token-value";

        Assert.NotEqual(raw, hasher.Hash(raw));
    }
}
