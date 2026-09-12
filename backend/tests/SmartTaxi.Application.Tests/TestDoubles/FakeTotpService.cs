using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>
/// Deterministic stand-in for RFC 6238 TOTP: the "correct" code for any secret
/// is always <c>$"code-for-{secret}"</c>, so tests can assert against it
/// without implementing real HMAC-SHA1/time-step math.
/// </summary>
public sealed class FakeTotpService : ITotpService
{
    private int _counter;

    public string GenerateSecret() => $"secret-{Interlocked.Increment(ref _counter)}";

    public string BuildAuthenticatorUri(string secret, string accountEmail, string issuer) =>
        $"otpauth://totp/{issuer}:{accountEmail}?secret={secret}&issuer={issuer}";

    public bool ValidateCode(string secret, string code, DateTime utcNow) => code == CodeFor(secret);

    public static string CodeFor(string secret) => $"code-for-{secret}";
}
