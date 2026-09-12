using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Application.Identity.Referrals;

public sealed record ReferralSummary(
    Guid Id,
    Guid ReferrerUserId,
    Guid RefereeUserId,
    ReferralStatus Status,
    DateTime CreatedAt,
    DateTime? ActivatedAt)
{
    public static ReferralSummary FromEntity(Referral referral) => new(
        referral.Id, referral.ReferrerUserId, referral.RefereeUserId, referral.Status, referral.CreatedAt, referral.ActivatedAt);
}
