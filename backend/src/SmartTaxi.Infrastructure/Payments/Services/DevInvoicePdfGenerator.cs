using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Infrastructure.Payments.Services;

/// <summary>
/// Abstraction-only placeholder, per instructions — no PDF rendering library
/// is integrated. Returns a deterministic storage key without writing any
/// file; a real implementation (e.g. QuestPDF-backed, persisted via
/// IFileStorageService) is a drop-in replacement behind IInvoicePdfGenerator.
/// </summary>
internal sealed class DevInvoicePdfGenerator : IInvoicePdfGenerator
{
    public Task<string> GenerateAsync(InvoicePdfData data, CancellationToken cancellationToken) =>
        Task.FromResult($"invoices/{data.InvoiceNumber}.pdf");
}
