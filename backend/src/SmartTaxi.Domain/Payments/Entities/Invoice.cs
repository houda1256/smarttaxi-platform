using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Payments.Entities;

/// <summary>One Invoice per Payment, generated at confirmation time. PdfStorageKey is set later, once IInvoicePdfGenerator has produced the document — never required at construction.</summary>
public sealed class Invoice : AggregateRoot
{
    public Guid PaymentId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string RideNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid OwnerId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; private set; }
    public DateTime IssueDate { get; private set; }
    public string? PdfStorageKey { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Snapshot of the TaxRule actually applied at issuance (Phase 5B) — null
    /// for Invoices generated before the tax catalog existed, or when no
    /// specific rule matched and the flat IInvoiceTaxPolicy fallback was
    /// used. A later change to TaxRule.TaxRate/IsActive must never alter an
    /// already-issued Invoice, which is exactly why this is a copy of the
    /// rule's values at the time, not a live foreign-key lookup.
    /// </summary>
    public Guid? TaxRuleId { get; private set; }
    public string? TaxRuleName { get; private set; }
    public decimal? TaxRateApplied { get; private set; }

    private Invoice()
    {
    }

    private Invoice(
        Guid paymentId, string rideNumber, Guid customerId, Guid driverId, Guid ownerId, decimal subtotal,
        decimal taxAmount, string currency, PaymentMethod paymentMethod, DateTime utcNow, Guid? taxRuleId,
        string? taxRuleName, decimal? taxRateApplied)
        : base(Guid.NewGuid())
    {
        PaymentId = paymentId;
        InvoiceNumber = GenerateInvoiceNumber(utcNow);
        RideNumber = rideNumber;
        CustomerId = customerId;
        DriverId = driverId;
        OwnerId = ownerId;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        TotalAmount = subtotal + taxAmount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        IssueDate = utcNow;
        CreatedAt = utcNow;
        TaxRuleId = taxRuleId;
        TaxRuleName = taxRuleName;
        TaxRateApplied = taxRateApplied;

        RaiseDomainEvent(new InvoiceGenerated(Id, paymentId, utcNow));
    }

    public static Invoice Generate(
        Guid paymentId, string rideNumber, Guid customerId, Guid driverId, Guid ownerId, decimal subtotal,
        decimal taxAmount, string currency, PaymentMethod paymentMethod, DateTime utcNow, Guid? taxRuleId = null,
        string? taxRuleName = null, decimal? taxRateApplied = null)
    {
        if (subtotal < 0)
        {
            throw new ArgumentException("Le sous-total ne peut pas être négatif.");
        }

        if (taxAmount < 0)
        {
            throw new ArgumentException("Le montant de taxe ne peut pas être négatif.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new Invoice(
            paymentId, rideNumber, customerId, driverId, ownerId, subtotal, taxAmount, currency.Trim().ToUpperInvariant(),
            paymentMethod, utcNow, taxRuleId, taxRuleName, taxRateApplied);
    }

    /// <summary>Single-actor field (only the PDF-generation step writes it) — no atomic guard needed.</summary>
    public void AttachPdf(string pdfStorageKey)
    {
        if (string.IsNullOrWhiteSpace(pdfStorageKey))
        {
            throw new ArgumentException("La clé de stockage du PDF est requise.");
        }

        PdfStorageKey = pdfStorageKey;
    }

    private static string GenerateInvoiceNumber(DateTime utcNow) =>
        $"INV-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
