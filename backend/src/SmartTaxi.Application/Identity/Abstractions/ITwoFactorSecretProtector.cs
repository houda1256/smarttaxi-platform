namespace SmartTaxi.Application.Identity.Abstractions;

/// <summary>
/// Encrypts/decrypts TOTP secrets at rest. Encryption (not hashing) is
/// deliberate: verifying a TOTP code requires recomputing it from the original
/// secret, which is impossible from a one-way hash.
/// </summary>
public interface ITwoFactorSecretProtector
{
    string Protect(string rawSecret);

    string Unprotect(string protectedSecret);
}
