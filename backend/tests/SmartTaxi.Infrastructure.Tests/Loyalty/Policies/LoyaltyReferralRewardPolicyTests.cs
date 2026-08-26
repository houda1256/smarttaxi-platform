using Microsoft.Extensions.Options;
using SmartTaxi.Infrastructure.Loyalty.Options;
using SmartTaxi.Infrastructure.Loyalty.Policies;

namespace SmartTaxi.Infrastructure.Tests.Loyalty.Policies;

public class LoyaltyReferralRewardPolicyTests
{
    [Fact]
    public void Properties_ReflectConfiguredOptions()
    {
        var policy = new LoyaltyReferralRewardPolicy(Options.Create(new LoyaltyOptions
        {
            ReferralReferrerRewardPoints = 200,
            ReferralRefereeRewardPoints = 75,
            ReferralRuleCode = "referral.custom"
        }));

        Assert.Equal(200, policy.ReferrerRewardPoints);
        Assert.Equal(75, policy.RefereeRewardPoints);
        Assert.Equal("referral.custom", policy.RuleCode);
    }
}
