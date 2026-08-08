using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Payments.Entities;

/// <summary>One Receipt per Payment, generated immediately once the Payment is confirmed.</summary>
public sealed class Receipt : AggregateRoot
{
    public Guid PaymentId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public string? PdfStorageKey { get; private set; }

    private Receipt()
    {
    }

    private Receipt(Guid paymentId, decimal amount, string currency, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        PaymentId = paymentId;
        ReceiptNumber = GenerateReceiptNumber(utcNow);
        Amount = amount;
        Currency = currency;
        IssuedAt = utcNow;

        RaiseDomainEvent(new ReceiptGenerated(Id, paymentId, utcNow));
    }

    public static Receipt Issue(Guid paymentId, decimal amount, string currency, DateTime utcNow)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Le montant ne peut pas être négatif.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new Receipt(paymentId, amount, currency.Trim().ToUpperInvariant(), utcNow);
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

    private static string GenerateReceiptNumber(DateTime utcNow) =>
        $"REC-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
