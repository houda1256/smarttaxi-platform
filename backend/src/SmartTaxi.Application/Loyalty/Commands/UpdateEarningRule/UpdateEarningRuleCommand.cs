using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.UpdateEarningRule;

public sealed record UpdateEarningRuleCommand(
    Guid RuleId, decimal RewardPointsPerCurrencyUnit, decimal StatusPointsPerCurrencyUnit, int? MinPoints, int? MaxPoints,
    bool SubscriptionMultiplierAllowed, DateTime? ValidFrom, DateTime? ValidTo) : ICommand<Result>;
