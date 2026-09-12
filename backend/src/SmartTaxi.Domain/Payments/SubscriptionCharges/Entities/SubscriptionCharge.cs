using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Enums;
using SmartTaxi.Domain.Payments.ValueObjects;

namespace SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;

/// <summary>
/// A single billing attempt for a Subscription Management plan charge —
/// deliberately separate from Payment (which is Ride-scoped: non-nullable
/// RideId, one-per-Ride unique index). A Subscription can accumulate many
/// SubscriptionCharges over its life (initial charge, each renewal), so the
/// index here is non-unique on SubscriptionId rather than a one-per-owner
/// guard. Status transitions are atomic repository-level guards, same
/// pattern as Payment.
/// </summary>
public sealed class SubscriptionCharge : AggregateRoot
{
    public Guid SubscriberId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public SubscriptionChargeStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SubscriptionCharge()
    {
    }

    private SubscriptionCharge(Guid subscriberId, Guid subscriptionId, Money amount, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        SubscriberId = subscriberId;
        SubscriptionId = subscriptionId;
        Amount = amount.Amount;
        Currency = amount.Currency;
        Status = SubscriptionChargeStatus.Pending;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static SubscriptionCharge Create(
        Guid subscriberId, Guid subscriptionId, decimal amount, string currency, DateTime utcNow)
    {
        var money = Money.Create(amount, currency);
        return new SubscriptionCharge(subscriberId, subscriptionId, money, utcNow);
    }
}
