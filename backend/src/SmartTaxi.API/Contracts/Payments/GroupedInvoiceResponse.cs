using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record GroupedInvoiceResponse(
    Guid Id,
    Guid BusinessCustomerId,
    string InvoiceNumber,
    string PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    DateTime CreatedAt)
{
    public static GroupedInvoiceResponse FromEntity(GroupedInvoice invoice) => new(
        invoice.Id, invoice.BusinessCustomerId, invoice.InvoiceNumber, invoice.PeriodType.ToString(), invoice.PeriodStart,
        invoice.PeriodEnd, invoice.Subtotal, invoice.TaxAmount, invoice.TotalAmount, invoice.Currency, invoice.IssueDate,
        invoice.DueDate, invoice.Status.ToString(), invoice.CreatedAt);
}

public sealed record GroupedInvoiceLineResponse(Guid Id, Guid GroupedInvoiceId, Guid RideId, string RideNumber, decimal Amount, DateTime CreatedAt)
{
    public static GroupedInvoiceLineResponse FromEntity(GroupedInvoiceLine line) => new(
        line.Id, line.GroupedInvoiceId, line.RideId, line.RideNumber, line.Amount, line.CreatedAt);
}
