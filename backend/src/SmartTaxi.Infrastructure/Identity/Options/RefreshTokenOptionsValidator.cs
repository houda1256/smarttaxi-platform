using Microsoft.Extensions.Options;

namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class RefreshTokenOptionsValidator : IValidateOptions<RefreshTokenOptions>
{
    private const int MaxLifetimeMinutes = 129_600; // 90 days
    private const int MaxSessionLifetimeDays = 365;

    public ValidateOptionsResult Validate(string? name, RefreshTokenOptions options)
    {
        var errors = new List<string>();

        if (options.LifetimeMinutes is <= 0 or > MaxLifetimeMinutes)
        {
            errors.Add($"RefreshToken:LifetimeMinutes must be between 1 and {MaxLifetimeMinutes}.");
        }

        if (options.SessionLifetimeDays is <= 0 or > MaxSessionLifetimeDays)
        {
            errors.Add($"RefreshToken:SessionLifetimeDays must be between 1 and {MaxSessionLifetimeDays}.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
