using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class TwoFactorRecoveryCodeRepository : ITwoFactorRecoveryCodeRepository
{
    private readonly ApplicationDbContext _context;

    public TwoFactorRecoveryCodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IReadOnlyCollection<TwoFactorRecoveryCode> codes, CancellationToken cancellationToken)
    {
        await _context.TwoFactorRecoveryCodes.AddRangeAsync(codes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TwoFactorRecoveryCode>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.TwoFactorRecoveryCodes
            .Where(code => code.UserId == userId && code.ConsumedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryConsumeAsync(Guid codeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.TwoFactorRecoveryCodes
            .Where(code => code.Id == codeId && code.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(code => code.ConsumedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.TwoFactorRecoveryCodes.Where(code => code.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}
