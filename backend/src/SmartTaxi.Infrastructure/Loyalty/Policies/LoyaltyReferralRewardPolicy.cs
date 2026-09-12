using Microsoft.Extensions.Options;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Infrastructure.Loyalty.Options;

namespace SmartTaxi.Infrastructure.Loyalty.Policies;

internal sealed class LoyaltyReferralRewardPolicy : ILoyaltyReferralRewardPolicy
{
    public int ReferrerRewardPoints { get; }

    public int RefereeRewardPoints { get; }

    public string RuleCode { get; }

    public LoyaltyReferralRewardPolicy(IOptions<LoyaltyOptions> options)
    {
        ReferrerRewardPoints = options.Value.ReferralReferrerRewardPoints;
        RefereeRewardPoints = options.Value.ReferralRefereeRewardPoints;
        RuleCode = options.Value.ReferralRuleCode;
    }
}
