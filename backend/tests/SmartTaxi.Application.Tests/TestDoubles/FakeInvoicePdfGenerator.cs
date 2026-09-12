using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeInvoicePdfGenerator : IInvoicePdfGenerator
{
    public Task<string> GenerateAsync(InvoicePdfData data, CancellationToken cancellationToken) =>
        Task.FromResult($"invoices/{data.InvoiceNumber}.pdf");
}
