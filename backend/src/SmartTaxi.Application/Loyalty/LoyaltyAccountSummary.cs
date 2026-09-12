using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty;

public sealed record LoyaltyAccountSummary(Guid AccountId, Guid UserId, string ActorRole, int CurrentRewardPoints, int CurrentStatusPoints, string Tier)
{
    public static LoyaltyAccountSummary FromEntity(LoyaltyAccount account) => new(
        account.Id, account.UserId, account.ActorRole.ToString(), account.CurrentRewardPoints, account.CurrentStatusPoints, account.Tier.ToString());
}
