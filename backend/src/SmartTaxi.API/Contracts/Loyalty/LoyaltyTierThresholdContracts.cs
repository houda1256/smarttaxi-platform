using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record UpdateTierThresholdRequest(int MinimumStatusPoints);

public sealed record LoyaltyTierThresholdResponse(string Tier, int MinimumStatusPoints)
{
    public static LoyaltyTierThresholdResponse FromEntity(LoyaltyTierThreshold threshold) => new(threshold.Tier.ToString(), threshold.MinimumStatusPoints);
}
