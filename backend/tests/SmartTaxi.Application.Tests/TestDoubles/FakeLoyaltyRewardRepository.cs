using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyRewardRepository : ILoyaltyRewardRepository
{
    private readonly Dictionary<Guid, LoyaltyReward> _rewards = new();

    public Task<LoyaltyReward?> GetByIdAsync(Guid rewardId, CancellationToken cancellationToken) =>
        Task.FromResult(_rewards.GetValueOrDefault(rewardId));

    public Task<LoyaltyReward?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_rewards.Values.FirstOrDefault(r => r.Code == code.Trim().ToUpper()));

    public Task<IReadOnlyCollection<LoyaltyReward>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyReward>>(_rewards.Values.ToList());

    public Task<IReadOnlyCollection<LoyaltyReward>> GetAvailableAsync(UserRole role, DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyReward>>(
            _rewards.Values.Where(r => r.IsAvailable(utcNow) && r.IsAvailableForRole(role)).ToList());

    public Task AddAsync(LoyaltyReward reward, CancellationToken cancellationToken)
    {
        _rewards[reward.Id] = reward;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyReward reward, CancellationToken cancellationToken)
    {
        _rewards[reward.Id] = reward;
        return Task.CompletedTask;
    }

    public Task<bool> TryIncrementRedeemedCountAsync(Guid rewardId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_rewards.TryGetValue(rewardId, out var reward))
        {
            return Task.FromResult(false);
        }

        if (reward.UsageLimit is not null && reward.RedeemedCount >= reward.UsageLimit)
        {
            return Task.FromResult(false);
        }

        reward.IncrementRedeemedCount(utcNow);
        return Task.FromResult(true);
    }
}
