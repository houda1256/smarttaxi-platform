namespace SmartTaxi.Domain.Loyalty.Enums;

/// <summary>
/// Two deliberately distinct concepts (never collapsed into one balance):
/// RewardPoints are spendable and may expire; StatusPoints only ever move a
/// LoyaltyAccount's Tier and are never redeemed or spent.
/// </summary>
public enum LoyaltyPointType
{
    RewardPoints,
    StatusPoints
}
