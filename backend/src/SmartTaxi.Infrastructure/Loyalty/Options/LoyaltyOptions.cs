namespace SmartTaxi.Infrastructure.Loyalty.Options;

public sealed class LoyaltyOptions
{
    public const string SectionName = "Loyalty";

    public int ReferralReferrerRewardPoints { get; init; } = 100;

    public int ReferralRefereeRewardPoints { get; init; } = 50;

    public string ReferralRuleCode { get; init; } = "referral.default";

    /// <summary>Mirrors the spec's Loyalty.PointExpirationMonths setting. 0 or less means RewardPoints never expire.</summary>
    public int PointExpirationMonths { get; init; } = 12;
}
