using SmartTaxi.Application.Loyalty.Commands.UpdateTierThreshold;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Loyalty;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.Loyalty.Commands;

public class UpdateTierThresholdCommandHandlerTests
{
    private readonly FakeLoyaltyTierThresholdRepository _repository = new();
    private readonly UpdateTierThresholdCommandHandler _handler;

    public UpdateTierThresholdCommandHandlerTests()
    {
        _handler = new UpdateTierThresholdCommandHandler(_repository);
    }

    private void SeedValidCatalog()
    {
        var utcNow = DateTime.UtcNow;
        _repository.Seed(
            LoyaltyTierThreshold.Create(LoyaltyTier.Bronze, 0, utcNow),
            LoyaltyTierThreshold.Create(LoyaltyTier.Silver, 2000, utcNow),
            LoyaltyTierThreshold.Create(LoyaltyTier.Gold, 5000, utcNow),
            LoyaltyTierThreshold.Create(LoyaltyTier.Platinum, 10000, utcNow));
    }

    [Fact]
    public async Task Handle_ValidStrictlyIncreasingOrdering_Succeeds()
    {
        SeedValidCatalog();

        var result = await _handler.Handle(new UpdateTierThresholdCommand(LoyaltyTier.Gold, 6000), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var thresholds = await _repository.GetAllAsync(CancellationToken.None);
        Assert.Equal(6000, thresholds.Single(t => t.Tier == LoyaltyTier.Gold).MinimumStatusPoints);
    }

    [Fact]
    public async Task Handle_GoldBelowSilver_IsRejected()
    {
        SeedValidCatalog();

        var result = await _handler.Handle(new UpdateTierThresholdCommand(LoyaltyTier.Gold, 500), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var thresholds = await _repository.GetAllAsync(CancellationToken.None);
        Assert.Equal(5000, thresholds.Single(t => t.Tier == LoyaltyTier.Gold).MinimumStatusPoints);
    }

    [Fact]
    public async Task Handle_PlatinumBelowGold_IsRejected()
    {
        SeedValidCatalog();

        var result = await _handler.Handle(new UpdateTierThresholdCommand(LoyaltyTier.Platinum, 4000), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var thresholds = await _repository.GetAllAsync(CancellationToken.None);
        Assert.Equal(10000, thresholds.Single(t => t.Tier == LoyaltyTier.Platinum).MinimumStatusPoints);
    }

    [Fact]
    public async Task Handle_NegativeThreshold_IsRejected()
    {
        SeedValidCatalog();

        var result = await _handler.Handle(new UpdateTierThresholdCommand(LoyaltyTier.Silver, -1), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_FirstTimeCreationOfPartialCatalog_OnlyComparesConfiguredTiers()
    {
        var result = await _handler.Handle(new UpdateTierThresholdCommand(LoyaltyTier.Gold, 5000), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidUpdate_TierCalculationRemainsDeterministic()
    {
        SeedValidCatalog();

        await _handler.Handle(new UpdateTierThresholdCommand(LoyaltyTier.Gold, 6000), CancellationToken.None);
        var thresholds = await _repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(LoyaltyTier.Bronze, LoyaltyTierCalculator.Determine(0, thresholds));
        Assert.Equal(LoyaltyTier.Silver, LoyaltyTierCalculator.Determine(2500, thresholds));
        Assert.Equal(LoyaltyTier.Silver, LoyaltyTierCalculator.Determine(5999, thresholds));
        Assert.Equal(LoyaltyTier.Gold, LoyaltyTierCalculator.Determine(6000, thresholds));
        Assert.Equal(LoyaltyTier.Platinum, LoyaltyTierCalculator.Determine(10000, thresholds));
    }
}
