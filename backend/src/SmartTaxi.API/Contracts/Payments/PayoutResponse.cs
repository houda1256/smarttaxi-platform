using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record PayoutResponse(
    Guid Id,
    Guid BeneficiaryAccountId,
    string BeneficiaryType,
    decimal Amount,
    string Currency,
    string Method,
    string Frequency,
    string Status,
    DateTime RequestedAt,
    Guid? ApprovedBy,
    DateTime? ApprovedAt,
    DateTime? ProcessedAt,
    DateTime? PaidAt,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static PayoutResponse FromEntity(Payout payout) => new(
        payout.Id, payout.BeneficiaryAccountId, payout.BeneficiaryType.ToString(), payout.Amount, payout.Currency,
        payout.Method.ToString(), payout.Frequency.ToString(), payout.Status.ToString(), payout.RequestedAt,
        payout.ApprovedBy, payout.ApprovedAt, payout.ProcessedAt, payout.PaidAt, payout.FailureReason, payout.CreatedAt,
        payout.UpdatedAt);
}
