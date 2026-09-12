using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Domain.Payments.Disputes.Entities;

/// <summary>
/// Opening a dispute atomically reserves DisputedAmount on the relevant
/// FinancialAccount's ReservedBalance (Application layer) so the funds can't
/// be paid out or spent while under review; resolution either releases the
/// reservation (amount returns to Available) or applies an adjustment
/// (amount is actually moved via a new, fully-audited ledger entry) — never
/// both, and never silently. Assignment/resolution are atomic
/// repository-level guards, not domain methods.
/// </summary>
public sealed class FinancialDispute : AggregateRoot
{
    public FinancialDisputeCategory Category { get; private set; }
    public Guid? RelatedPaymentId { get; private set; }
    public Guid? RelatedInvoiceId { get; private set; }
    public Guid? RelatedPayoutId { get; private set; }
    public decimal DisputedAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? EvidenceReference { get; private set; }
    public FinancialDisputeStatus Status { get; private set; }
    public Guid RaisedBy { get; private set; }
    public Guid? AssignedFinanceManagerId { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private FinancialDispute()
    {
    }

    private FinancialDispute(
        FinancialDisputeCategory category, Guid? relatedPaymentId, Guid? relatedInvoiceId, Guid? relatedPayoutId,
        decimal disputedAmount, string currency, string description, string? evidenceReference, Guid raisedBy, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Category = category;
        RelatedPaymentId = relatedPaymentId;
        RelatedInvoiceId = relatedInvoiceId;
        RelatedPayoutId = relatedPayoutId;
        DisputedAmount = disputedAmount;
        Currency = currency;
        Description = description;
        EvidenceReference = evidenceReference;
        Status = FinancialDisputeStatus.Open;
        RaisedBy = raisedBy;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static FinancialDispute Open(
        FinancialDisputeCategory category, Guid? relatedPaymentId, Guid? relatedInvoiceId, Guid? relatedPayoutId,
        decimal disputedAmount, string currency, string description, string? evidenceReference, Guid raisedBy, DateTime utcNow)
    {
        if (relatedPaymentId is null && relatedInvoiceId is null && relatedPayoutId is null)
        {
            throw new ArgumentException("Un litige doit référencer au moins un paiement, une facture ou un versement.");
        }

        if (disputedAmount <= 0)
        {
            throw new ArgumentException("Le montant contesté doit être positif.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Une description est requise.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new FinancialDispute(
            category, relatedPaymentId, relatedInvoiceId, relatedPayoutId, disputedAmount, currency.Trim().ToUpperInvariant(),
            description, evidenceReference, raisedBy, utcNow);
    }
}
