using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments;

public sealed record PaymentSummary(
    Guid Id,
    Guid RideId,
    string RideNumber,
    Guid CustomerId,
    Guid DriverId,
    Guid VehicleId,
    Guid OwnerId,
    string PaymentReference,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    decimal? EstimatedFareAmount,
    decimal FinalFareAmount,
    string Currency,
    decimal? PlatformCommissionAmount,
    decimal? DriverAmount,
    decimal? OwnerAmount,
    decimal RefundedAmount,
    DateTime CreatedAt,
    DateTime? AuthorizedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime? FailedAt,
    DateTime UpdatedAt)
{
    public static PaymentSummary FromEntity(Payment payment) => new(
        payment.Id, payment.RideId, payment.RideNumber, payment.CustomerId, payment.DriverId, payment.VehicleId,
        payment.OwnerId, payment.PaymentReference, payment.PaymentMethod, payment.Status, payment.EstimatedFareAmount,
        payment.FinalFareAmount, payment.Currency, payment.PlatformCommissionAmount, payment.DriverAmount,
        payment.OwnerAmount, payment.RefundedAmount, payment.CreatedAt, payment.AuthorizedAt, payment.ConfirmedAt,
        payment.CancelledAt, payment.FailedAt, payment.UpdatedAt);
}
