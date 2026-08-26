using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyRedemptionRepository : ILoyaltyRedemptionRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyRedemptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoyaltyRedemption redemption, CancellationToken cancellationToken)
    {
        await _context.LoyaltyRedemptions.AddAsync(redemption, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<LoyaltyRedemption?> GetByIdAsync(Guid redemptionId, CancellationToken cancellationToken) =>
        _context.LoyaltyRedemptions.FirstOrDefaultAsync(redemption => redemption.Id == redemptionId, cancellationToken);

    public async Task<PagedResult<LoyaltyRedemption>> GetForUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.LoyaltyRedemptions.Where(redemption => redemption.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(redemption => redemption.RedeemedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoyaltyRedemption>(items, totalCount, pageNumber, pageSize);
    }
}
