using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyRewardRepository : ILoyaltyRewardRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyRewardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyReward?> GetByIdAsync(Guid rewardId, CancellationToken cancellationToken) =>
        _context.LoyaltyRewards.FirstOrDefaultAsync(reward => reward.Id == rewardId, cancellationToken);

    public Task<LoyaltyReward?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.LoyaltyRewards.FirstOrDefaultAsync(reward => reward.Code == code.Trim().ToUpper(), cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyReward>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.LoyaltyRewards.OrderBy(reward => reward.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyReward>> GetAvailableAsync(UserRole role, DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = await _context.LoyaltyRewards.Where(reward => reward.IsActive).ToListAsync(cancellationToken);
        return candidates.Where(reward => reward.IsAvailable(utcNow) && reward.IsAvailableForRole(role)).ToList();
    }

    public async Task AddAsync(LoyaltyReward reward, CancellationToken cancellationToken)
    {
        await _context.LoyaltyRewards.AddAsync(reward, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoyaltyReward reward, CancellationToken cancellationToken)
    {
        _context.LoyaltyRewards.Update(reward);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryIncrementRedeemedCountAsync(Guid rewardId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.LoyaltyRewards
            .Where(reward => reward.Id == rewardId && (reward.UsageLimit == null || reward.RedeemedCount < reward.UsageLimit))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(reward => reward.RedeemedCount, reward => reward.RedeemedCount + 1)
                .SetProperty(reward => reward.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }
}
