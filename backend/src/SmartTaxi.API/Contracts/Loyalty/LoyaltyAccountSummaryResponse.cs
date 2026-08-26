using SmartTaxi.Application.Loyalty;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyAccountSummaryResponse(Guid AccountId, string ActorRole, int CurrentRewardPoints, int CurrentStatusPoints, string Tier)
{
    public static LoyaltyAccountSummaryResponse FromSummary(LoyaltyAccountSummary summary) => new(
        summary.AccountId, summary.ActorRole, summary.CurrentRewardPoints, summary.CurrentStatusPoints, summary.Tier);
}
