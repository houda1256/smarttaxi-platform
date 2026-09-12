using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyTierThresholdRepository : ILoyaltyTierThresholdRepository
{
    private readonly Dictionary<Guid, LoyaltyTierThreshold> _thresholds = new();

    public Task<LoyaltyTierThreshold?> GetByTierAsync(LoyaltyTier tier, CancellationToken cancellationToken) =>
        Task.FromResult(_thresholds.Values.FirstOrDefault(t => t.Tier == tier));

    public Task<IReadOnlyCollection<LoyaltyTierThreshold>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyTierThreshold>>(_thresholds.Values.ToList());

    public Task AddAsync(LoyaltyTierThreshold threshold, CancellationToken cancellationToken)
    {
        _thresholds[threshold.Id] = threshold;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyTierThreshold threshold, CancellationToken cancellationToken)
    {
        _thresholds[threshold.Id] = threshold;
        return Task.CompletedTask;
    }

    public void Seed(params LoyaltyTierThreshold[] thresholds)
    {
        foreach (var threshold in thresholds)
        {
            _thresholds[threshold.Id] = threshold;
        }
    }
}
