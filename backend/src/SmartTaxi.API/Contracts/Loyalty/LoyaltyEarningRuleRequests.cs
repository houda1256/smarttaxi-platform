namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record CreateEarningRuleRequest(
    string Code, string ActorRole, string SourceType, decimal RewardPointsPerCurrencyUnit, decimal StatusPointsPerCurrencyUnit,
    int? MinPoints, int? MaxPoints, bool SubscriptionMultiplierAllowed, DateTime? ValidFrom, DateTime? ValidTo);

public sealed record UpdateEarningRuleRequest(
    decimal RewardPointsPerCurrencyUnit, decimal StatusPointsPerCurrencyUnit, int? MinPoints, int? MaxPoints,
    bool SubscriptionMultiplierAllowed, DateTime? ValidFrom, DateTime? ValidTo);
