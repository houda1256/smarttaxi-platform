using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface ISessionRepository
{
    Task AddAsync(UserSession session, CancellationToken cancellationToken);

    Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the session owning the refresh token with this hash, searching its
    /// entire rotation chain (not just the current token) so a reused, already
    /// rotated token can still be located and trigger reuse detection.
    /// </summary>
    Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserSession>> GetActiveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists mutations to an already-loaded session. Returns false if a
    /// concurrent update won the race (optimistic concurrency conflict) instead
    /// of throwing, since this is an expected, routine outcome under contention.
    /// </summary>
    Task<bool> UpdateAsync(UserSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically revokes every currently-active session for a user in a single
    /// bulk operation (used by "revoke all sessions", and by password reset/
    /// change) — a single SQL statement is inherently atomic, so no separate
    /// transaction or per-row loop is needed. When <paramref name="exceptSessionId"/>
    /// is supplied (password change only — the session that performed the
    /// change), that one session is left untouched; password reset always
    /// passes null since there is no "current session" in that unauthenticated flow.
    /// </summary>
    Task<int> RevokeAllActiveSessionsAsync(
        Guid userId,
        SessionRevocationReason reason,
        DateTime utcNow,
        CancellationToken cancellationToken,
        Guid? exceptSessionId = null);
}
