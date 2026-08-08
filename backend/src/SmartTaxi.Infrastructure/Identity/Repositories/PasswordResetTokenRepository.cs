using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.PasswordResetTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken) =>
        _context.PasswordResetTokens
            .Where(token => token.UserId == userId && token.ConsumedAt == null && token.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ExpiresAt, utcNow), cancellationToken);

    public async Task<bool> TryConsumeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.PasswordResetTokens
            .Where(token => token.Id == tokenId && token.ConsumedAt == null && token.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ConsumedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
