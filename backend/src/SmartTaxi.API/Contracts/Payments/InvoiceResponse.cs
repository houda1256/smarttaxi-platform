using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record InvoiceResponse(
    Guid Id, Guid PaymentId, string InvoiceNumber, string RideNumber, Guid CustomerId, Guid DriverId, Guid OwnerId,
    decimal Subtotal, decimal TaxAmount, decimal TotalAmount, string Currency, string PaymentMethod, DateTime IssueDate,
    string? PdfStorageKey)
{
    public static InvoiceResponse FromEntity(Invoice invoice) => new(
        invoice.Id, invoice.PaymentId, invoice.InvoiceNumber, invoice.RideNumber, invoice.CustomerId, invoice.DriverId,
        invoice.OwnerId, invoice.Subtotal, invoice.TaxAmount, invoice.TotalAmount, invoice.Currency,
        invoice.PaymentMethod.ToString(), invoice.IssueDate, invoice.PdfStorageKey);
}
