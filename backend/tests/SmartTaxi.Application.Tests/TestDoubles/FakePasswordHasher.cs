using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "hashed:";

    public string Hash(string rawPassword) => Prefix + rawPassword;

    public bool Verify(string hashedPassword, string providedPassword)
        => hashedPassword == Prefix + providedPassword;
}
