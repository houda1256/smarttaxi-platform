namespace SmartTaxi.Infrastructure.Configuration;

/// <summary>
/// Six policies is the smallest set that safely covers the system's actual
/// risk shape (see the Module 13B design audit) — every value here is a
/// configurable default, never a magic number buried in endpoint-mapping code.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowPolicyOptions AuthCritical { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public FixedWindowPolicyOptions OtpAndChallenge { get; init; } = new() { PermitLimit = 5, WindowSeconds = 300 };

    public FixedWindowPolicyOptions PublicRead { get; init; } = new() { PermitLimit = 60, WindowSeconds = 60 };

    public SlidingWindowPolicyOptions AuthenticatedGeneral { get; init; } =
        new() { PermitLimit = 120, WindowSeconds = 60, SegmentsPerWindow = 6 };

    public SlidingWindowPolicyOptions AdminMutation { get; init; } =
        new() { PermitLimit = 30, WindowSeconds = 60, SegmentsPerWindow = 6 };

    public TokenBucketPolicyOptions SensitiveMutation { get; init; } =
        new() { TokenLimit = 10, TokensPerPeriod = 1, ReplenishmentPeriodSeconds = 6 };
}

public sealed class FixedWindowPolicyOptions
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; }

    public int QueueLimit { get; init; }
}

public sealed class SlidingWindowPolicyOptions
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; }

    public int SegmentsPerWindow { get; init; }

    public int QueueLimit { get; init; }
}

public sealed class TokenBucketPolicyOptions
{
    public int TokenLimit { get; init; }

    public int TokensPerPeriod { get; init; }

    public int ReplenishmentPeriodSeconds { get; init; }

    public bool AutoReplenishment { get; init; } = true;

    public int QueueLimit { get; init; }
}
