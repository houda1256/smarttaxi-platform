using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyReferralRewardPolicy : ILoyaltyReferralRewardPolicy
{
    public int ReferrerRewardPoints { get; set; } = 100;

    public int RefereeRewardPoints { get; set; } = 50;

    public string RuleCode { get; set; } = "referral.default";
}
