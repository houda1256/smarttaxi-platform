using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record RefundRecordResponse(
    Guid Id, Guid PaymentId, decimal Amount, string Currency, string RefundType, string Reason, Guid RequestedBy, DateTime ProcessedAt)
{
    public static RefundRecordResponse FromEntity(RefundRecord record) => new(
        record.Id, record.PaymentId, record.Amount, record.Currency, record.RefundType.ToString(), record.Reason,
        record.RequestedBy, record.ProcessedAt);
}
