namespace SmartTaxi.Domain.Loyalty.Enums;

/// <summary>
/// Deliberately limited to criteria Loyalty can evaluate from its own data —
/// both are derived by counting the user's own ledger effects (RideCount from
/// Earn entries sourced from a Payment; ReferralCount from LoyaltyReferralReward
/// rows where the user is the referrer) rather than reading Rides/Identity
/// tables directly, per "only implement criteria supported by existing source
/// events/data."
/// </summary>
public enum LoyaltyChallengeCriteriaType
{
    RideCount,
    ReferralCount
}
