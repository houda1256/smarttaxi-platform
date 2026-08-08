using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Payments.Taxes.Entities;

/// <summary>
/// Replaces the flat IInvoiceTaxPolicy percentage from Phase 5A with a real
/// catalog. Deactivation (IsActive -> false) is an atomic repository-level
/// guard; existing fields are otherwise immutable once created — an
/// "amendment" is always a new TaxRule row (mirrors DriverOwnerContract's own
/// versioned-amendment convention), never an in-place edit, precisely
/// because Invoices snapshot the applied rule's Id/Name/Rate at issuance and
/// must never be retroactively affected by a later change.
/// </summary>
public sealed class TaxRule : AggregateRoot
{
    public string TaxName { get; private set; } = string.Empty;
    public decimal TaxRate { get; private set; }
    public string Jurisdiction { get; private set; } = string.Empty;
    public string ApplicableService { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public string? ExemptionRules { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TaxRule()
    {
    }

    private TaxRule(
        string taxName, decimal taxRate, string jurisdiction, string applicableService, DateOnly effectiveFrom,
        DateOnly? effectiveTo, string? exemptionRules, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        TaxName = taxName;
        TaxRate = taxRate;
        Jurisdiction = jurisdiction;
        ApplicableService = applicableService;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
        ExemptionRules = exemptionRules;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static TaxRule Create(
        string taxName, decimal taxRate, string jurisdiction, string applicableService, DateOnly effectiveFrom,
        DateOnly? effectiveTo, string? exemptionRules, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(taxName))
        {
            throw new ArgumentException("Le nom de la taxe est requis.");
        }

        if (taxRate < 0)
        {
            throw new ArgumentException("Le taux de taxe ne peut pas être négatif.");
        }

        if (effectiveTo is not null && effectiveTo < effectiveFrom)
        {
            throw new ArgumentException("La date de fin d'application ne peut pas précéder la date de début.");
        }

        return new TaxRule(taxName, taxRate, jurisdiction, applicableService, effectiveFrom, effectiveTo, exemptionRules, utcNow);
    }

    public bool IsApplicableOn(DateOnly date, string service) =>
        IsActive && EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date)
        && (ApplicableService == "All" || string.Equals(ApplicableService, service, StringComparison.OrdinalIgnoreCase));
}
