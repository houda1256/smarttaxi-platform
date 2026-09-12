using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.LoyaltyAccounts.FirstOrDefaultAsync(account => account.UserId == userId, cancellationToken);

    public Task<LoyaltyAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        _context.LoyaltyAccounts.FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public async Task<bool> TryAddAsync(LoyaltyAccount account, CancellationToken cancellationToken)
    {
        await _context.LoyaltyAccounts.AddAsync(account, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(account).State = EntityState.Detached;
            return false;
        }
    }
}
