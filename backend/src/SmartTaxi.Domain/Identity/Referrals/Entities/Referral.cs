using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Domain.Identity.Referrals.Entities;

public sealed class Referral
{
    public Guid Id { get; private set; }
    public Guid ReferrerUserId { get; private set; }
    public Guid RefereeUserId { get; private set; }
    public string ReferralCodeUsed { get; private set; } = string.Empty;
    public ReferralStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Referral()
    {
    }

    public Referral(Guid referrerUserId, Guid refereeUserId, string referralCodeUsed, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        ReferrerUserId = referrerUserId;
        RefereeUserId = refereeUserId;
        ReferralCodeUsed = referralCodeUsed;
        Status = ReferralStatus.PendingActivation;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
