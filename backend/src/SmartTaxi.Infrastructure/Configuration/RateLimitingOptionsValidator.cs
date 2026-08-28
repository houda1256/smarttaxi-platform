using Microsoft.Extensions.Options;

namespace SmartTaxi.Infrastructure.Configuration;

/// <summary>Rejects zero/negative and pathologically large values — never echoes any value back (these aren't secrets, but errors stay generic regardless).</summary>
public sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    private const int MaxLimit = 100_000;
    private const int MaxWindowSeconds = 86_400;
    private const int MaxSegments = 1_000;

    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var errors = new List<string>();

        ValidateFixedWindow(nameof(options.AuthCritical), options.AuthCritical, errors);
        ValidateFixedWindow(nameof(options.OtpAndChallenge), options.OtpAndChallenge, errors);
        ValidateFixedWindow(nameof(options.PublicRead), options.PublicRead, errors);
        ValidateSlidingWindow(nameof(options.AuthenticatedGeneral), options.AuthenticatedGeneral, errors);
        ValidateSlidingWindow(nameof(options.AdminMutation), options.AdminMutation, errors);
        ValidateTokenBucket(nameof(options.SensitiveMutation), options.SensitiveMutation, errors);

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateFixedWindow(string policyName, FixedWindowPolicyOptions policy, List<string> errors)
    {
        if (policy.PermitLimit is <= 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:PermitLimit must be between 1 and {MaxLimit}.");
        }

        if (policy.WindowSeconds is <= 0 or > MaxWindowSeconds)
        {
            errors.Add($"RateLimiting:{policyName}:WindowSeconds must be between 1 and {MaxWindowSeconds}.");
        }

        if (policy.QueueLimit is < 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:QueueLimit must be between 0 and {MaxLimit}.");
        }
    }

    private static void ValidateSlidingWindow(string policyName, SlidingWindowPolicyOptions policy, List<string> errors)
    {
        if (policy.PermitLimit is <= 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:PermitLimit must be between 1 and {MaxLimit}.");
        }

        if (policy.WindowSeconds is <= 0 or > MaxWindowSeconds)
        {
            errors.Add($"RateLimiting:{policyName}:WindowSeconds must be between 1 and {MaxWindowSeconds}.");
        }

        if (policy.SegmentsPerWindow is <= 0 or > MaxSegments)
        {
            errors.Add($"RateLimiting:{policyName}:SegmentsPerWindow must be between 1 and {MaxSegments}.");
        }

        if (policy.QueueLimit is < 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:QueueLimit must be between 0 and {MaxLimit}.");
        }
    }

    private static void ValidateTokenBucket(string policyName, TokenBucketPolicyOptions policy, List<string> errors)
    {
        if (policy.TokenLimit is <= 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:TokenLimit must be between 1 and {MaxLimit}.");
        }

        if (policy.TokensPerPeriod is <= 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:TokensPerPeriod must be between 1 and {MaxLimit}.");
        }

        if (policy.ReplenishmentPeriodSeconds is <= 0 or > MaxWindowSeconds)
        {
            errors.Add($"RateLimiting:{policyName}:ReplenishmentPeriodSeconds must be between 1 and {MaxWindowSeconds}.");
        }

        if (policy.QueueLimit is < 0 or > MaxLimit)
        {
            errors.Add($"RateLimiting:{policyName}:QueueLimit must be between 0 and {MaxLimit}.");
        }
    }
}
