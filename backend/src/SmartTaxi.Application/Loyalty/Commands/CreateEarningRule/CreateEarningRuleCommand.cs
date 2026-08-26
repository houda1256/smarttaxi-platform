using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.CreateEarningRule;

public sealed record CreateEarningRuleCommand(
    string Code, UserRole ActorRole, string SourceType, decimal RewardPointsPerCurrencyUnit, decimal StatusPointsPerCurrencyUnit,
    int? MinPoints, int? MaxPoints, bool SubscriptionMultiplierAllowed, DateTime? ValidFrom, DateTime? ValidTo) : ICommand<Result<Guid>>;
