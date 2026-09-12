using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record PaymentTransactionHistoryResponse(
    Guid Id, Guid PaymentId, string? PreviousStatus, string NewStatus, Guid? ChangedBy, string? Reason, decimal? Amount, DateTime ChangedAt)
{
    public static PaymentTransactionHistoryResponse FromEntity(PaymentTransactionHistory history) => new(
        history.Id, history.PaymentId, history.PreviousStatus?.ToString(), history.NewStatus.ToString(), history.ChangedBy,
        history.Reason, history.Amount, history.ChangedAt);
}
