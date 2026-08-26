using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyTierThresholdRepository : ILoyaltyTierThresholdRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyTierThresholdRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyTierThreshold?> GetByTierAsync(LoyaltyTier tier, CancellationToken cancellationToken) =>
        _context.LoyaltyTierThresholds.FirstOrDefaultAsync(threshold => threshold.Tier == tier, cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyTierThreshold>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.LoyaltyTierThresholds.OrderBy(threshold => threshold.MinimumStatusPoints).ToListAsync(cancellationToken);

    public async Task AddAsync(LoyaltyTierThreshold threshold, CancellationToken cancellationToken)
    {
        await _context.LoyaltyTierThresholds.AddAsync(threshold, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoyaltyTierThreshold threshold, CancellationToken cancellationToken)
    {
        _context.LoyaltyTierThresholds.Update(threshold);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
