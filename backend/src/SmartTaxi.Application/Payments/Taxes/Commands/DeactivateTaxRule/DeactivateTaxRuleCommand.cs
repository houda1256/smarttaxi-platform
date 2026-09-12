using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Taxes.Commands.DeactivateTaxRule;

public sealed record DeactivateTaxRuleCommand(Guid TaxRuleId) : ICommand<Result>;
