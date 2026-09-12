using SmartTaxi.Application.Identity.Referrals;

namespace SmartTaxi.API.Contracts.Identity.Referrals;

public sealed record ReferralResponse(
    Guid Id, Guid ReferrerUserId, Guid RefereeUserId, string Status, DateTime CreatedAt, DateTime? ActivatedAt)
{
    public static ReferralResponse FromSummary(ReferralSummary summary) => new(
        summary.Id, summary.ReferrerUserId, summary.RefereeUserId, summary.Status.ToString(), summary.CreatedAt, summary.ActivatedAt);
}
