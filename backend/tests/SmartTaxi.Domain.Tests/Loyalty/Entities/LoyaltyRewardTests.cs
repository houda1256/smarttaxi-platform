using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyRewardTests
{
    private static LoyaltyReward NewReward(
        LoyaltyRewardType type = LoyaltyRewardType.FreeService, IReadOnlyList<UserRole>? roles = null, int? usageLimit = null,
        DateTime? availableFrom = null, DateTime? availableTo = null) =>
        LoyaltyReward.Create("FREE_WASH", "Lavage gratuit", "desc", 100, type, roles ?? [], availableFrom, availableTo, usageLimit, DateTime.UtcNow);

    [Theory]
    [InlineData(LoyaltyRewardType.FreeService, true)]
    [InlineData(LoyaltyRewardType.PartnerOffer, true)]
    [InlineData(LoyaltyRewardType.PromotionalCoupon, true)]
    [InlineData(LoyaltyRewardType.RideDiscount, false)]
    [InlineData(LoyaltyRewardType.SubscriptionDiscount, false)]
    public void IsExecutable_MatchesRewardType(LoyaltyRewardType type, bool expectedExecutable)
    {
        var reward = NewReward(type);

        Assert.Equal(expectedExecutable, reward.IsExecutable);
    }

    [Fact]
    public void IsAvailable_WhenInactive_ReturnsFalse()
    {
        var reward = NewReward();
        reward.Deactivate(DateTime.UtcNow);

        Assert.False(reward.IsAvailable(DateTime.UtcNow));
    }

    [Fact]
    public void IsAvailable_BeforeAvailableFrom_ReturnsFalse()
    {
        var utcNow = DateTime.UtcNow;
        var reward = NewReward(availableFrom: utcNow.AddDays(1));

        Assert.False(reward.IsAvailable(utcNow));
    }

    [Fact]
    public void IsAvailable_AfterUsageLimitReached_ReturnsFalse()
    {
        var reward = NewReward(usageLimit: 1);
        reward.IncrementRedeemedCount(DateTime.UtcNow);

        Assert.False(reward.IsAvailable(DateTime.UtcNow));
    }

    [Fact]
    public void IsAvailableForRole_WithNoTargetRoles_AllowsAnyRole()
    {
        var reward = NewReward(roles: []);

        Assert.True(reward.IsAvailableForRole(UserRole.Driver));
    }

    [Fact]
    public void IsAvailableForRole_WithTargetRoles_RestrictsToThem()
    {
        var reward = NewReward(roles: [UserRole.Customer]);

        Assert.True(reward.IsAvailableForRole(UserRole.Customer));
        Assert.False(reward.IsAvailableForRole(UserRole.Driver));
    }

    [Fact]
    public void Create_WithNonPositiveCost_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyReward.Create(
            "CODE", "Name", "desc", 0, LoyaltyRewardType.FreeService, [], null, null, null, DateTime.UtcNow));
    }
}
