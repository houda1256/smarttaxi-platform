using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyRedemptionTests
{
    [Fact]
    public void Create_WithPositiveCost_RaisesRewardRedeemed()
    {
        var userId = Guid.NewGuid();
        var rewardId = Guid.NewGuid();

        var redemption = LoyaltyRedemption.Create(Guid.NewGuid(), userId, rewardId, 100, "key-1", DateTime.UtcNow);

        var raised = Assert.Single(redemption.DomainEvents);
        var evt = Assert.IsType<RewardRedeemed>(raised);
        Assert.Equal(userId, evt.UserId);
        Assert.Equal(rewardId, evt.RewardId);
    }

    [Fact]
    public void Create_WithNonPositiveCost_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyRedemption.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "key-1", DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithBlankIdempotencyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyRedemption.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, " ", DateTime.UtcNow));
    }
}
