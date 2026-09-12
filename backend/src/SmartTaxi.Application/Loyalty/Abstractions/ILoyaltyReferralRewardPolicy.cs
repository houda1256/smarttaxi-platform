namespace SmartTaxi.Application.Loyalty.Abstractions;

/// <summary>Admin-configurable via IOptions&lt;T&gt; (same convention as NotificationOptions/PayoutOptions) rather than a full rule-catalog entity — referral reward is a single platform-wide policy, not a per-event-varying rate.</summary>
public interface ILoyaltyReferralRewardPolicy
{
    int ReferrerRewardPoints { get; }

    int RefereeRewardPoints { get; }

    string RuleCode { get; }
}
