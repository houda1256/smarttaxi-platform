using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyRewardResponse(
    Guid Id, string Code, string Name, string Description, int CostInRewardPoints, string RewardType, bool IsExecutable,
    IReadOnlyCollection<string> TargetRoles, bool IsActive, DateTime? AvailableFrom, DateTime? AvailableTo, int? UsageLimit, int RedeemedCount)
{
    public static LoyaltyRewardResponse FromEntity(LoyaltyReward reward) => new(
        reward.Id, reward.Code, reward.Name, reward.Description, reward.CostInRewardPoints, reward.RewardType.ToString(), reward.IsExecutable,
        reward.TargetRoles.Select(role => role.ToString()).ToList(), reward.IsActive, reward.AvailableFrom, reward.AvailableTo,
        reward.UsageLimit, reward.RedeemedCount);
}
