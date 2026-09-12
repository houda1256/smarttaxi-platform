using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyEarningRuleResponse(
    Guid Id, string Code, string ActorRole, string SourceType, decimal RewardPointsPerCurrencyUnit, decimal StatusPointsPerCurrencyUnit,
    int? MinPoints, int? MaxPoints, bool SubscriptionMultiplierAllowed, bool IsActive, DateTime? ValidFrom, DateTime? ValidTo)
{
    public static LoyaltyEarningRuleResponse FromEntity(LoyaltyEarningRule rule) => new(
        rule.Id, rule.Code, rule.ActorRole.ToString(), rule.SourceType, rule.RewardPointsPerCurrencyUnit, rule.StatusPointsPerCurrencyUnit,
        rule.MinPoints, rule.MaxPoints, rule.SubscriptionMultiplierAllowed, rule.IsActive, rule.ValidFrom, rule.ValidTo);
}
