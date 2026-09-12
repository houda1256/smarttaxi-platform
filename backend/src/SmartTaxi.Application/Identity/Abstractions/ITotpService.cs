namespace SmartTaxi.Application.Identity.Abstractions;

public interface ITotpService
{
    /// <summary>Generates a new raw (unencrypted) Base32 TOTP secret.</summary>
    string GenerateSecret();

    /// <summary>Builds the otpauth:// URI an authenticator app can consume.</summary>
    string BuildAuthenticatorUri(string secret, string accountEmail, string issuer);

    /// <summary>Validates a submitted code against the secret, allowing a small clock-drift window.</summary>
    bool ValidateCode(string secret, string code, DateTime utcNow);
}
