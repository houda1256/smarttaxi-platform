using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTwoFactorSecretProtector : ITwoFactorSecretProtector
{
    private const string Prefix = "protected:";

    public string Protect(string rawSecret) => Prefix + rawSecret;

    public string Unprotect(string protectedSecret) => protectedSecret.StartsWith(Prefix, StringComparison.Ordinal)
        ? protectedSecret[Prefix.Length..]
        : protectedSecret;
}
