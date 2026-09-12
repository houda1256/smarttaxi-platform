using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Events;
using SmartTaxi.Domain.Payments.ValueObjects;

namespace SmartTaxi.Domain.Payments.Entities;

/// <summary>
/// One Payment per Ride at a time — the DB-level defense against duplicate
/// payments is a partial unique index on RideId WHERE Status is
/// non-terminal-and-not-Cancelled/Failed (Infrastructure); this entity only
/// enforces its own shape. Status transitions (Authorize/Confirm/Fail/
/// Cancel/Refund) are atomic repository-level guards, never domain mutation
/// methods — same pattern as every Ride/Fleet status transition, since more
/// than one actor (Customer, Driver, Admin, a future gateway callback) could
/// race a transition. DriverAmount/OwnerAmount/PlatformCommissionAmount are
/// computed once, at confirmation, and never recomputed afterward.
/// </summary>
public sealed class Payment : AggregateRoot
{
    public Guid RideId { get; private set; }
    public string RideNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid OwnerId { get; private set; }

    public string PaymentReference { get; private set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; }

    public decimal? EstimatedFareAmount { get; private set; }
    public decimal FinalFareAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public decimal? PlatformCommissionAmount { get; private set; }
    public decimal? DriverAmount { get; private set; }
    public decimal? OwnerAmount { get; private set; }
    public decimal RefundedAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Payment()
    {
    }

    private Payment(
        Guid rideId, string rideNumber, Guid customerId, Guid driverId, Guid vehicleId, Guid ownerId,
        PaymentMethod paymentMethod, decimal? estimatedFareAmount, Money finalFare, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        RideNumber = rideNumber;
        CustomerId = customerId;
        DriverId = driverId;
        VehicleId = vehicleId;
        OwnerId = ownerId;
        PaymentReference = GeneratePaymentReference(utcNow);
        PaymentMethod = paymentMethod;
        Status = PaymentStatus.Pending;
        EstimatedFareAmount = estimatedFareAmount;
        FinalFareAmount = finalFare.Amount;
        Currency = finalFare.Currency;
        RefundedAmount = 0m;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new PaymentCreated(Id, rideId, customerId, utcNow));
    }

    public static Payment Create(
        Guid rideId, string rideNumber, Guid customerId, Guid driverId, Guid vehicleId, Guid ownerId,
        PaymentMethod paymentMethod, decimal? estimatedFareAmount, decimal finalFareAmount, string currency, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(rideNumber))
        {
            throw new ArgumentException("Le numéro de course est requis.");
        }

        if (!Money.TryCreate(finalFareAmount, currency, out var finalFare, out var error))
        {
            throw new ArgumentException(error);
        }

        if (estimatedFareAmount is < 0)
        {
            throw new ArgumentException("Le montant estimé ne peut pas être négatif.");
        }

        return new Payment(rideId, rideNumber, customerId, driverId, vehicleId, ownerId, paymentMethod, estimatedFareAmount, finalFare, utcNow);
    }

    private static string GeneratePaymentReference(DateTime utcNow) =>
        $"PAY-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    public decimal RemainingRefundableAmount => FinalFareAmount - RefundedAmount;
}
