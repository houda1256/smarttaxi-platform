using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyReferralRewardRepository : ILoyaltyReferralRewardRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyReferralRewardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyReferralReward?> GetByReferralIdAsync(Guid referralId, CancellationToken cancellationToken) =>
        _context.LoyaltyReferralRewards.FirstOrDefaultAsync(reward => reward.ReferralId == referralId, cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyReferralReward>> GetForReferrerAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
        await _context.LoyaltyReferralRewards.Where(reward => reward.ReferrerUserId == referrerUserId).ToListAsync(cancellationToken);

    public async Task<bool> TryAddAsync(LoyaltyReferralReward reward, CancellationToken cancellationToken)
    {
        await _context.LoyaltyReferralRewards.AddAsync(reward, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(reward).State = EntityState.Detached;
            return false;
        }
    }
}
