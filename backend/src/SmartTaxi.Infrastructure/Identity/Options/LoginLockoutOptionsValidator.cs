using Microsoft.Extensions.Options;

namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class LoginLockoutOptionsValidator : IValidateOptions<LoginLockoutOptions>
{
    private const int MaxFailedAttemptsCeiling = 20;
    private const int MaxLockoutMinutes = 1_440; // 24 hours

    public ValidateOptionsResult Validate(string? name, LoginLockoutOptions options)
    {
        var errors = new List<string>();

        if (options.MaxFailedAttempts is <= 0 or > MaxFailedAttemptsCeiling)
        {
            errors.Add($"LoginLockout:MaxFailedAttempts must be between 1 and {MaxFailedAttemptsCeiling}.");
        }

        if (options.LockoutMinutes is <= 0 or > MaxLockoutMinutes)
        {
            errors.Add($"LoginLockout:LockoutMinutes must be between 1 and {MaxLockoutMinutes}.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
