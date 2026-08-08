namespace SmartTaxi.Domain.Identity.Entities;

public sealed class TwoFactorRecoveryCode
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    private TwoFactorRecoveryCode()
    {
    }

    public TwoFactorRecoveryCode(Guid userId, string codeHash, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CodeHash = codeHash;
        CreatedAt = utcNow;
    }

    public bool IsValid() => ConsumedAt is null;
}
