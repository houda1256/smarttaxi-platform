using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Abstractions;

/// <summary>Abstraction only, per instructions — no real PDF rendering library is integrated; the Infrastructure implementation is a documented dev stub.</summary>
public interface IInvoicePdfGenerator
{
    Task<string> GenerateAsync(InvoicePdfData data, CancellationToken cancellationToken);
}

public sealed record InvoicePdfData(
    string InvoiceNumber, string RideNumber, Guid CustomerId, Guid DriverId, Guid OwnerId, decimal Subtotal,
    decimal TaxAmount, decimal TotalAmount, string Currency, PaymentMethod PaymentMethod, DateTime IssueDate);
