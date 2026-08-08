using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record ReceiptResponse(Guid Id, Guid PaymentId, string ReceiptNumber, decimal Amount, string Currency, DateTime IssuedAt, string? PdfStorageKey)
{
    public static ReceiptResponse FromEntity(Receipt receipt) => new(
        receipt.Id, receipt.PaymentId, receipt.ReceiptNumber, receipt.Amount, receipt.Currency, receipt.IssuedAt, receipt.PdfStorageKey);
}
