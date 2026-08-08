using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Domain.Payments.Entities;

/// <summary>Immutable, append-only — written by the same atomic call that performs the transition it records. This is the audit trail the master prompt requires for every financial action.</summary>
public sealed class PaymentTransactionHistory
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public PaymentStatus? PreviousStatus { get; private set; }
    public PaymentStatus NewStatus { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? Reason { get; private set; }
    public decimal? Amount { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private PaymentTransactionHistory()
    {
    }

    public PaymentTransactionHistory(
        Guid paymentId, PaymentStatus? previousStatus, PaymentStatus newStatus, Guid? changedBy, string? reason,
        decimal? amount, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        PaymentId = paymentId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        Reason = reason;
        Amount = amount;
        ChangedAt = utcNow;
    }
}
