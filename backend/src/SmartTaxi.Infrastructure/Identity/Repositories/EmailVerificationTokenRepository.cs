using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;

    public EmailVerificationTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.EmailVerificationTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task<DateTime?> GetLastIssuedAtAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.EmailVerificationTokens
            .Where(token => token.UserId == userId)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => (DateTime?)token.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken) =>
        _context.EmailVerificationTokens
            .Where(token => token.UserId == userId && token.ConsumedAt == null && token.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ExpiresAt, utcNow), cancellationToken);

    public async Task<bool> TryConsumeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.EmailVerificationTokens
            .Where(token => token.Id == tokenId && token.ConsumedAt == null && token.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ConsumedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
