using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Identity.Entities;

public sealed class UserSession : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = [];

    public Guid UserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime LastActivityAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public SessionRevocationReason? RevokedReason { get; private set; }

    public string? DeviceLabel { get; private set; }

    /// <summary>
    /// Application-managed optimistic concurrency token, regenerated on every
    /// mutation. Used (instead of a database-computed token) so "two concurrent
    /// refresh attempts cannot both succeed" is enforced portably at the EF Core
    /// level without depending on a specific database provider's system columns.
    /// </summary>
    public Guid ConcurrencyStamp { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens;

    private UserSession(Guid id, Guid userId, DateTime createdAt, DateTime expiresAt, string? deviceLabel)
        : base(id)
    {
        UserId = userId;
        CreatedAt = createdAt;
        LastActivityAt = createdAt;
        ExpiresAt = expiresAt;
        DeviceLabel = deviceLabel;
    }

    public static UserSession Start(
        Guid userId,
        string initialTokenHash,
        DateTime tokenExpiresAt,
        DateTime sessionExpiresAt,
        string? deviceLabel,
        DateTime utcNow)
    {
        var session = new UserSession(Guid.NewGuid(), userId, utcNow, sessionExpiresAt, deviceLabel)
        {
            ConcurrencyStamp = Guid.NewGuid()
        };
        var initialToken = new RefreshToken(session.Id, initialTokenHash, tokenExpiresAt, utcNow);
        session._refreshTokens.Add(initialToken);

        return session;
    }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;

    /// <summary>
    /// Attempts to rotate the refresh token identified by <paramref name="presentedTokenHash"/>.
    /// This is the single place the reuse-detection rule is enforced: presenting a
    /// token that was already rotated or revoked revokes the entire session (the
    /// whole "token family") as a side effect, so no caller can forget to do it.
    /// </summary>
    public TokenRotationResult RotateToken(
        string presentedTokenHash, string newTokenHash, DateTime newTokenExpiresAt, DateTime utcNow)
    {
        if (RevokedAt is not null)
        {
            return TokenRotationResult.Failure(TokenRotationOutcome.SessionRevoked);
        }

        if (utcNow >= ExpiresAt)
        {
            return TokenRotationResult.Failure(TokenRotationOutcome.SessionExpired);
        }

        var presentedToken = _refreshTokens.FirstOrDefault(token => token.TokenHash == presentedTokenHash);

        if (presentedToken is null)
        {
            return TokenRotationResult.Failure(TokenRotationOutcome.TokenNotFound);
        }

        // Only IsReplaced is checked here (not IsRevoked): a token can only become
        // revoked as part of revoking its whole session, and that's already handled
        // by the RevokedAt check above — a revoked-but-not-replaced token can never
        // reach this line. RefreshToken.RevokedAt still exists as an audit stamp on
        // the specific token that triggered detection, set just below.
        if (presentedToken.IsReplaced)
        {
            presentedToken.Revoke(utcNow);
            Revoke(SessionRevocationReason.ReuseDetected, utcNow);
            return TokenRotationResult.Failure(TokenRotationOutcome.ReuseDetected);
        }

        if (presentedToken.IsExpired(utcNow))
        {
            return TokenRotationResult.Failure(TokenRotationOutcome.TokenExpired);
        }

        var newToken = new RefreshToken(Id, newTokenHash, newTokenExpiresAt, utcNow);
        presentedToken.MarkReplacedBy(newToken.Id);
        _refreshTokens.Add(newToken);
        LastActivityAt = utcNow;
        ConcurrencyStamp = Guid.NewGuid();

        return TokenRotationResult.Success(newToken);
    }

    public void Revoke(SessionRevocationReason reason, DateTime utcNow)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = utcNow;
        RevokedReason = reason;
        ConcurrencyStamp = Guid.NewGuid();
    }
}
