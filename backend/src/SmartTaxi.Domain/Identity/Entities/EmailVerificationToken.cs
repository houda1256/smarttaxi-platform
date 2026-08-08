namespace SmartTaxi.Domain.Identity.Entities;

public sealed class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    private EmailVerificationToken()
    {
    }

    public EmailVerificationToken(Guid userId, string tokenHash, DateTime expiresAt, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsValid(DateTime utcNow) => ConsumedAt is null && utcNow < ExpiresAt;
}
