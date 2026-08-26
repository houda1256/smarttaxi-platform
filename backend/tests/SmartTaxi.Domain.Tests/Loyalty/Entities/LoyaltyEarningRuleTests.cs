using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyEarningRuleTests
{
    private static LoyaltyEarningRule NewRule(
        decimal rewardRate = 1m, decimal statusRate = 1m, int? min = null, int? max = null, bool multiplierAllowed = true) =>
        LoyaltyEarningRule.Create(
            "RIDE_PAID", UserRole.Customer, "Payment", rewardRate, statusRate, min, max, multiplierAllowed, null, null, DateTime.UtcNow);

    [Fact]
    public void CalculatePoints_FloorsFractionalPoints()
    {
        var rule = NewRule(rewardRate: 1m);

        var (rewardPoints, _) = rule.CalculatePoints(9.99m, 100m);

        Assert.Equal(9, rewardPoints);
    }

    [Fact]
    public void CalculatePoints_AppliesSubscriptionMultiplierWhenAllowed()
    {
        var rule = NewRule(rewardRate: 1m, multiplierAllowed: true);

        var (rewardPoints, _) = rule.CalculatePoints(10m, 150m);

        Assert.Equal(15, rewardPoints);
    }

    [Fact]
    public void CalculatePoints_IgnoresMultiplierWhenNotAllowed()
    {
        var rule = NewRule(rewardRate: 1m, multiplierAllowed: false);

        var (rewardPoints, _) = rule.CalculatePoints(10m, 200m);

        Assert.Equal(10, rewardPoints);
    }

    [Fact]
    public void CalculatePoints_ClampsToMinPoints()
    {
        var rule = NewRule(rewardRate: 0.01m, min: 5);

        var (rewardPoints, _) = rule.CalculatePoints(1m, 100m);

        Assert.Equal(5, rewardPoints);
    }

    [Fact]
    public void CalculatePoints_ClampsToMaxPoints()
    {
        var rule = NewRule(rewardRate: 10m, max: 20);

        var (rewardPoints, _) = rule.CalculatePoints(100m, 100m);

        Assert.Equal(20, rewardPoints);
    }

    [Fact]
    public void Create_WithMaxLessThanMin_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyEarningRule.Create(
            "CODE", UserRole.Customer, "Payment", 1m, 1m, 10, 5, true, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void IsEffective_OutsideValidityWindow_ReturnsFalse()
    {
        var utcNow = DateTime.UtcNow;
        var rule = LoyaltyEarningRule.Create(
            "CODE", UserRole.Customer, "Payment", 1m, 1m, null, null, true, utcNow.AddDays(1), null, utcNow);

        Assert.False(rule.IsEffective(utcNow));
    }

    [Fact]
    public void Deactivate_MakesRuleNotEffective()
    {
        var rule = NewRule();
        rule.Deactivate(DateTime.UtcNow);

        Assert.False(rule.IsEffective(DateTime.UtcNow));
    }
}
