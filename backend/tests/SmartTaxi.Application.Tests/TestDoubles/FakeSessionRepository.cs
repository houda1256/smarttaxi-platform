using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, UserSession> _sessionsById = new();

    public Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        _sessionsById[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
        => Task.FromResult(_sessionsById.GetValueOrDefault(sessionId));

    public Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var session = _sessionsById.Values
            .FirstOrDefault(s => s.RefreshTokens.Any(token => token.TokenHash == tokenHash));

        return Task.FromResult(session);
    }

    public Task<IReadOnlyCollection<UserSession>> GetActiveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        IReadOnlyCollection<UserSession> sessions = _sessionsById.Values
            .Where(s => s.UserId == userId && s.IsActive(utcNow))
            .ToList();

        return Task.FromResult(sessions);
    }

    public Task<bool> UpdateAsync(UserSession session, CancellationToken cancellationToken)
    {
        _sessionsById[session.Id] = session;
        return Task.FromResult(true);
    }

    public Task<int> RevokeAllActiveSessionsAsync(
        Guid userId,
        SessionRevocationReason reason,
        DateTime utcNow,
        CancellationToken cancellationToken,
        Guid? exceptSessionId = null)
    {
        var count = 0;

        foreach (var session in _sessionsById.Values.Where(s =>
                     s.UserId == userId && s.IsActive(utcNow) && s.Id != exceptSessionId))
        {
            session.Revoke(reason, utcNow);
            count++;
        }

        return Task.FromResult(count);
    }
}
