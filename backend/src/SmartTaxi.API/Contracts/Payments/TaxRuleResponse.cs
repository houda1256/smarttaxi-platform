using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record TaxRuleResponse(
    Guid Id,
    string TaxName,
    decimal TaxRate,
    string Jurisdiction,
    string ApplicableService,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? ExemptionRules,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static TaxRuleResponse FromEntity(TaxRule rule) => new(
        rule.Id, rule.TaxName, rule.TaxRate, rule.Jurisdiction, rule.ApplicableService, rule.EffectiveFrom, rule.EffectiveTo,
        rule.IsActive, rule.ExemptionRules, rule.CreatedAt, rule.UpdatedAt);
}
