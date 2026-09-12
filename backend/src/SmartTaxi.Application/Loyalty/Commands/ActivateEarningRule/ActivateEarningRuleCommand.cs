using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.ActivateEarningRule;

public sealed record ActivateEarningRuleCommand(Guid RuleId) : ICommand<Result>;
