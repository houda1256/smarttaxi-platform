using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyRewardRepository
{
    Task<LoyaltyReward?> GetByIdAsync(Guid rewardId, CancellationToken cancellationToken);

    Task<LoyaltyReward?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyReward>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyReward>> GetAvailableAsync(UserRole role, DateTime utcNow, CancellationToken cancellationToken);

    Task AddAsync(LoyaltyReward reward, CancellationToken cancellationToken);

    Task UpdateAsync(LoyaltyReward reward, CancellationToken cancellationToken);

    /// <summary>Atomic conditional increment guarded on UsageLimit (NULL or RedeemedCount &lt; UsageLimit) — the mechanism that prevents over-redeeming a limited-stock reward under concurrency.</summary>
    Task<bool> TryIncrementRedeemedCountAsync(Guid rewardId, DateTime utcNow, CancellationToken cancellationToken);
}
