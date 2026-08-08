namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class ReferralActivationOptions
{
    public const string SectionName = "ReferralActivation";

    public bool RequireEmailVerified { get; init; } = true;

    public bool RequirePhoneVerified { get; init; }

    public int MinAccountAgeDays { get; init; }
}
