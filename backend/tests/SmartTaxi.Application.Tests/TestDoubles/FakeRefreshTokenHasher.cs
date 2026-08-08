using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
{
    private const string Prefix = "hashed:";

    public string Hash(string rawToken) => Prefix + rawToken;
}
