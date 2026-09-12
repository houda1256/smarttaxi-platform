using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyAccountTests
{
    private static IReadOnlyCollection<LoyaltyTierThreshold> Thresholds(DateTime utcNow) =>
    [
        LoyaltyTierThreshold.Create(LoyaltyTier.Bronze, 0, utcNow),
        LoyaltyTierThreshold.Create(LoyaltyTier.Silver, 500, utcNow),
        LoyaltyTierThreshold.Create(LoyaltyTier.Gold, 2000, utcNow)
    ];

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Driver)]
    public void Open_WithEligibleRole_Succeeds(UserRole role)
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), role, DateTime.UtcNow);

        Assert.Equal(role, account.ActorRole);
        Assert.Equal(LoyaltyTier.Bronze, account.Tier);
        Assert.Single(account.DomainEvents, e => e is LoyaltyAccountCreated);
    }

    [Fact]
    public void Open_WithIneligibleRole_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Admin, DateTime.UtcNow));
    }

    [Fact]
    public void ApplyEarn_IncrementsBothBalances()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;

        account.ApplyEarn(10, 5, Thresholds(utcNow), utcNow);

        Assert.Equal(10, account.CurrentRewardPoints);
        Assert.Equal(5, account.CurrentStatusPoints);
    }

    [Fact]
    public void ApplyEarn_CrossingThreshold_RaisesTierChanged()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;

        account.ApplyEarn(0, 600, Thresholds(utcNow), utcNow);

        Assert.Equal(LoyaltyTier.Silver, account.Tier);
        Assert.Contains(account.DomainEvents, e => e is LoyaltyTierChanged changed && changed.NewTier == LoyaltyTier.Silver);
    }

    [Fact]
    public void ApplyEarn_NegativePoints_Throws()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => account.ApplyEarn(-1, 0, [], DateTime.UtcNow));
    }

    [Fact]
    public void ApplyRewardPointsDelta_BelowZero_Throws()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => account.ApplyRewardPointsDelta(-1, DateTime.UtcNow));
    }

    [Fact]
    public void ApplyRewardPointsDelta_PositiveThenNegative_NetsCorrectly()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        account.ApplyRewardPointsDelta(50, DateTime.UtcNow);

        account.ApplyRewardPointsDelta(-30, DateTime.UtcNow);

        Assert.Equal(20, account.CurrentRewardPoints);
    }
}
