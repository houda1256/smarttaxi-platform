using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Taxes.Commands.CreateTaxRule;

public sealed record CreateTaxRuleCommand(
    string TaxName, decimal TaxRate, string Jurisdiction, string ApplicableService, DateOnly EffectiveFrom,
    DateOnly? EffectiveTo, string? ExemptionRules) : ICommand<Result<Guid>>;
