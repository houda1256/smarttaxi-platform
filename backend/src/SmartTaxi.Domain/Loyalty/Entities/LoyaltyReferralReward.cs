using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>
/// Loyalty's own record that one Identity Referral was rewarded — Identity
/// owns the Referral relationship and its RewardEligible status; Loyalty owns
/// only this outcome row, unique on ReferralId, so the same referral can never
/// be rewarded twice regardless of how many times the evaluation is retried.
/// </summary>
public sealed class LoyaltyReferralReward : AggregateRoot
{
    public Guid ReferralId { get; private set; }
    public Guid ReferrerUserId { get; private set; }
    public Guid RefereeUserId { get; private set; }
    public int ReferrerRewardPoints { get; private set; }
    public int RefereeRewardPoints { get; private set; }
    public string RuleCode { get; private set; } = string.Empty;
    public DateTime GrantedAtUtc { get; private set; }

    private LoyaltyReferralReward()
    {
    }

    private LoyaltyReferralReward(
        Guid referralId, Guid referrerUserId, Guid refereeUserId, int referrerRewardPoints, int refereeRewardPoints,
        string ruleCode, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        ReferralId = referralId;
        ReferrerUserId = referrerUserId;
        RefereeUserId = refereeUserId;
        ReferrerRewardPoints = referrerRewardPoints;
        RefereeRewardPoints = refereeRewardPoints;
        RuleCode = ruleCode;
        GrantedAtUtc = utcNow;

        RaiseDomainEvent(new ReferralRewardGranted(Id, referralId, referrerUserId, refereeUserId, utcNow));
    }

    public static LoyaltyReferralReward Grant(
        Guid referralId, Guid referrerUserId, Guid refereeUserId, int referrerRewardPoints, int refereeRewardPoints, string ruleCode,
        DateTime utcNow)
    {
        if (referrerRewardPoints < 0 || refereeRewardPoints < 0)
        {
            throw new ArgumentException("Les points de récompense de parrainage ne peuvent pas être négatifs.");
        }

        if (referrerRewardPoints == 0 && refereeRewardPoints == 0)
        {
            throw new ArgumentException("Au moins une des deux parties doit recevoir des points.");
        }

        if (string.IsNullOrWhiteSpace(ruleCode))
        {
            throw new ArgumentException("Une référence de règle est requise.");
        }

        return new LoyaltyReferralReward(referralId, referrerUserId, refereeUserId, referrerRewardPoints, refereeRewardPoints, ruleCode.Trim(), utcNow);
    }
}
