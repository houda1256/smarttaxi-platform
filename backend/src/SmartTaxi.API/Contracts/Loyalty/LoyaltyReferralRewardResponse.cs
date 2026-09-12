using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyReferralRewardResponse(
    Guid ReferralId, Guid ReferrerUserId, Guid RefereeUserId, int ReferrerRewardPoints, int RefereeRewardPoints, DateTime GrantedAtUtc)
{
    public static LoyaltyReferralRewardResponse FromEntity(LoyaltyReferralReward reward) => new(
        reward.ReferralId, reward.ReferrerUserId, reward.RefereeUserId, reward.ReferrerRewardPoints, reward.RefereeRewardPoints, reward.GrantedAtUtc);
}
