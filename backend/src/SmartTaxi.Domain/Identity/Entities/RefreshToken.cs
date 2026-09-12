namespace SmartTaxi.Domain.Identity.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public Guid? ReplacedByRefreshTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsReplaced => ReplacedByRefreshTokenId is not null;

    private RefreshToken()
    {
    }

    internal RefreshToken(Guid sessionId, string tokenHash, DateTime expiresAt, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        TokenHash = tokenHash;
        CreatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    internal void MarkReplacedBy(Guid replacementTokenId)
    {
        ReplacedByRefreshTokenId = replacementTokenId;
    }

    internal void Revoke(DateTime utcNow)
    {
        RevokedAt ??= utcNow;
    }
}
