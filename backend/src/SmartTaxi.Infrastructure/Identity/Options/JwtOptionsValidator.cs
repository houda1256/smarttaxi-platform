using Microsoft.Extensions.Options;

namespace SmartTaxi.Infrastructure.Identity.Options;

/// <summary>
/// Defense-in-depth for the DI-bound JwtOptions consumed by JwtTokenGenerator.
/// Program.cs additionally fails fast on an empty Key before AddJwtBearer even
/// runs (it needs the raw value synchronously, before DI is built) — this
/// validator covers the same options through the normal ValidateOnStart path
/// and adds the checks that startup check doesn't: key strength, Issuer/
/// Audience, and a sane expiry range. Never echoes the key itself.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MinimumKeyLength = 32; // ~256 bits for HS256, per RFC 7518 guidance
    private const int MaxExpiryMinutes = 10_080; // 7 days

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            errors.Add("Jwt:Key must be configured.");
        }
        else if (options.Key.Length < MinimumKeyLength)
        {
            errors.Add($"Jwt:Key must be at least {MinimumKeyLength} characters long.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            errors.Add("Jwt:Audience must be configured.");
        }

        if (options.ExpiryMinutes is <= 0 or > MaxExpiryMinutes)
        {
            errors.Add($"Jwt:ExpiryMinutes must be between 1 and {MaxExpiryMinutes}.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
