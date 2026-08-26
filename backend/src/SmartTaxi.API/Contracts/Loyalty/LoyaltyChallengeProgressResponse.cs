using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyChallengeProgressResponse(Guid ChallengeId, int CurrentValue, DateTime? CompletedAtUtc, DateTime? RewardedAtUtc)
{
    public static LoyaltyChallengeProgressResponse FromEntity(LoyaltyChallengeProgress progress) => new(
        progress.ChallengeId, progress.CurrentValue, progress.CompletedAtUtc, progress.RewardedAtUtc);
}
