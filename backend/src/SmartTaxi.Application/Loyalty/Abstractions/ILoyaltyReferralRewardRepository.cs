using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyReferralRewardRepository
{
    Task<LoyaltyReferralReward?> GetByReferralIdAsync(Guid referralId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyReferralReward>> GetForReferrerAsync(Guid referrerUserId, CancellationToken cancellationToken);

    /// <summary>False when the unique ReferralId index rejects a concurrent duplicate grant — the mechanism that guarantees one Identity referral is never rewarded twice.</summary>
    Task<bool> TryAddAsync(LoyaltyReferralReward reward, CancellationToken cancellationToken);
}
