namespace SmartTaxi.Domain.Loyalty.Enums;

/// <summary>Corrections use AdminAdjustment (positive or negative Points), never editing or deleting a historical row — see LoyaltyPointLedgerEntry.</summary>
public enum LoyaltyLedgerEntryType
{
    Earn,
    Redeem,
    Expire,
    ReferralReward,
    ChallengeReward,
    AdminAdjustment
}
