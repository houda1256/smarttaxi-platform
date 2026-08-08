using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Payments.Entities;

/// <summary>Immutable, append-only audit trail for every refund — the master prompt requires every refund operation to be audited, so this row is the audit itself, never edited or deleted.</summary>
public sealed class RefundRecord : AggregateRoot
{
    public Guid PaymentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public RefundType RefundType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid RequestedBy { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private RefundRecord()
    {
    }

    private RefundRecord(Guid paymentId, decimal amount, string currency, RefundType refundType, string reason, Guid requestedBy, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        RefundType = refundType;
        Reason = reason;
        RequestedBy = requestedBy;
        ProcessedAt = utcNow;

        RaiseDomainEvent(new RefundIssued(Id, paymentId, amount, utcNow));
    }

    public static RefundRecord Issue(Guid paymentId, decimal amount, string currency, RefundType refundType, string reason, Guid requestedBy, DateTime utcNow)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Le montant du remboursement doit être positif.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Un motif est requis pour un remboursement.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new RefundRecord(paymentId, amount, currency.Trim().ToUpperInvariant(), refundType, reason, requestedBy, utcNow);
    }
}
