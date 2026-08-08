namespace SmartTaxi.Domain.Identity.Entities;

public sealed class PhoneVerificationOtp
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string OtpHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    private PhoneVerificationOtp()
    {
    }

    public PhoneVerificationOtp(Guid userId, string otpHash, DateTime expiresAt, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        OtpHash = otpHash;
        CreatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsLocked(DateTime utcNow) => LockedUntil is not null && utcNow < LockedUntil;

    public bool IsValid(DateTime utcNow) => ConsumedAt is null && utcNow < ExpiresAt && !IsLocked(utcNow);
}
