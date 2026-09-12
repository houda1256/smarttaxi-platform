using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyTierThresholdRepository
{
    Task<LoyaltyTierThreshold?> GetByTierAsync(LoyaltyTier tier, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyTierThreshold>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(LoyaltyTierThreshold threshold, CancellationToken cancellationToken);

    Task UpdateAsync(LoyaltyTierThreshold threshold, CancellationToken cancellationToken);
}
