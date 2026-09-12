namespace SmartTaxi.API.Contracts.Payments;

public sealed record CreateTaxRuleRequest(
    string TaxName, decimal TaxRate, string Jurisdiction, string ApplicableService, DateOnly EffectiveFrom,
    DateOnly? EffectiveTo, string? ExemptionRules);
