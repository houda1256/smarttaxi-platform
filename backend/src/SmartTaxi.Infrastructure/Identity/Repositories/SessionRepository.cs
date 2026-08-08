using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class SessionRepository : ISessionRepository
{
    private readonly ApplicationDbContext _context;

    public SessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        await _context.Sessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _context.Sessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public async Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return await _context.Sessions
            .FirstOrDefaultAsync(session => session.RefreshTokens.Any(token => token.TokenHash == tokenHash), cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserSession>> GetActiveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        return await _context.Sessions
            .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(UserSession session, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<int> RevokeAllActiveSessionsAsync(
        Guid userId,
        SessionRevocationReason reason,
        DateTime utcNow,
        CancellationToken cancellationToken,
        Guid? exceptSessionId = null)
    {
        return await _context.Sessions
            .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > utcNow
                && (exceptSessionId == null || session.Id != exceptSessionId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAt, utcNow)
                    .SetProperty(session => session.RevokedReason, reason),
                cancellationToken);
    }
}
