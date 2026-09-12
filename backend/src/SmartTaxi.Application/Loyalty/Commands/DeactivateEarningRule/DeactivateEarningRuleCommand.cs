using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.DeactivateEarningRule;

public sealed record DeactivateEarningRuleCommand(Guid RuleId) : ICommand<Result>;
