using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyReferralRewardRepository : ILoyaltyReferralRewardRepository
{
    private readonly Dictionary<Guid, LoyaltyReferralReward> _rewards = new();

    public Task<LoyaltyReferralReward?> GetByReferralIdAsync(Guid referralId, CancellationToken cancellationToken) =>
        Task.FromResult(_rewards.Values.FirstOrDefault(r => r.ReferralId == referralId));

    public Task<IReadOnlyCollection<LoyaltyReferralReward>> GetForReferrerAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyReferralReward>>(_rewards.Values.Where(r => r.ReferrerUserId == referrerUserId).ToList());

    public Task<bool> TryAddAsync(LoyaltyReferralReward reward, CancellationToken cancellationToken)
    {
        if (_rewards.Values.Any(r => r.ReferralId == reward.ReferralId))
        {
            return Task.FromResult(false);
        }

        _rewards[reward.Id] = reward;
        return Task.FromResult(true);
    }
}
