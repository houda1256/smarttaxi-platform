namespace SmartTaxi.Domain.Loyalty.Enums;

/// <summary>
/// The audit proved Payments has no discount/voucher/price-adjustment hook.
/// FreeService/PartnerOffer/PromotionalCoupon are fully executable by Loyalty
/// alone (see LoyaltyReward.IsExecutable). RideDiscount/SubscriptionDiscount
/// are modeled for catalog completeness (the spec requires them) but are
/// deliberately NOT executable today — redeeming one is rejected with a clear
/// error rather than silently pretending a fare/subscription discount was
/// applied. Wiring them is a documented future Payments/Subscriptions
/// integration, not part of this module.
/// </summary>
public enum LoyaltyRewardType
{
    FreeService,
    PartnerOffer,
    PromotionalCoupon,
    RideDiscount,
    SubscriptionDiscount
}
