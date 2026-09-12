using SmartTaxi.Application.Payments;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid RideId,
    string RideNumber,
    Guid CustomerId,
    Guid DriverId,
    Guid VehicleId,
    Guid OwnerId,
    string PaymentReference,
    string PaymentMethod,
    string Status,
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
    public static PaymentResponse FromSummary(PaymentSummary summary) => new(
        summary.Id, summary.RideId, summary.RideNumber, summary.CustomerId, summary.DriverId, summary.VehicleId,
        summary.OwnerId, summary.PaymentReference, summary.PaymentMethod.ToString(), summary.Status.ToString(),
        summary.EstimatedFareAmount, summary.FinalFareAmount, summary.Currency, summary.PlatformCommissionAmount,
        summary.DriverAmount, summary.OwnerAmount, summary.RefundedAmount, summary.CreatedAt, summary.AuthorizedAt,
        summary.ConfirmedAt, summary.CancelledAt, summary.FailedAt, summary.UpdatedAt);
}
