namespace SmartTaxi.Domain.Identity.Entities;

public sealed class TwoFactorChallenge
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? DeviceLabel { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    private TwoFactorChallenge()
    {
    }

    public TwoFactorChallenge(Guid userId, string tokenHash, string? deviceLabel, DateTime expiresAt, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        DeviceLabel = deviceLabel;
        CreatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsValid(DateTime utcNow) => ConsumedAt is null && utcNow < ExpiresAt;
}
