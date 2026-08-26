using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyReferralRewardTests
{
    [Fact]
    public void Grant_WithAtLeastOnePositiveAmount_RaisesReferralRewardGranted()
    {
        var referralId = Guid.NewGuid();

        var reward = LoyaltyReferralReward.Grant(referralId, Guid.NewGuid(), Guid.NewGuid(), 100, 50, "referral.default", DateTime.UtcNow);

        var raised = Assert.Single(reward.DomainEvents);
        var evt = Assert.IsType<ReferralRewardGranted>(raised);
        Assert.Equal(referralId, evt.ReferralId);
    }

    [Fact]
    public void Grant_WithBothAmountsZero_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyReferralReward.Grant(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, 0, "referral.default", DateTime.UtcNow));
    }

    [Fact]
    public void Grant_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyReferralReward.Grant(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1, 50, "referral.default", DateTime.UtcNow));
    }
}
