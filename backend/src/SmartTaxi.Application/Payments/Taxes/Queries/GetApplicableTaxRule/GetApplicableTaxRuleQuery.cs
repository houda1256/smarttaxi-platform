using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Payments.Taxes.Queries.GetApplicableTaxRule;

public sealed record GetApplicableTaxRuleQuery(string ApplicableService, DateOnly Date) : IQuery<TaxRule?>;
