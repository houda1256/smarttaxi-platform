using SmartTaxi.Domain.Loyalty;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Tests.Loyalty;

public class LoyaltyTierCalculatorTests
{
    private static readonly IReadOnlyCollection<LoyaltyTierThreshold> Thresholds =
    [
        LoyaltyTierThreshold.Create(LoyaltyTier.Bronze, 0, DateTime.UtcNow),
        LoyaltyTierThreshold.Create(LoyaltyTier.Silver, 500, DateTime.UtcNow),
        LoyaltyTierThreshold.Create(LoyaltyTier.Gold, 2000, DateTime.UtcNow),
        LoyaltyTierThreshold.Create(LoyaltyTier.Platinum, 5000, DateTime.UtcNow)
    ];

    [Theory]
    [InlineData(0, LoyaltyTier.Bronze)]
    [InlineData(499, LoyaltyTier.Bronze)]
    [InlineData(500, LoyaltyTier.Silver)]
    [InlineData(1999, LoyaltyTier.Silver)]
    [InlineData(2000, LoyaltyTier.Gold)]
    [InlineData(5000, LoyaltyTier.Platinum)]
    [InlineData(999999, LoyaltyTier.Platinum)]
    public void Determine_ReturnsHighestReachedTier(int statusPoints, LoyaltyTier expectedTier)
    {
        Assert.Equal(expectedTier, LoyaltyTierCalculator.Determine(statusPoints, Thresholds));
    }

    [Fact]
    public void Determine_WithNoThresholdsConfigured_FallsBackToBronze()
    {
        Assert.Equal(LoyaltyTier.Bronze, LoyaltyTierCalculator.Determine(10000, []));
    }
}
