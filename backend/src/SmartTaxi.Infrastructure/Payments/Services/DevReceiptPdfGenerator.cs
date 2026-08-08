using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Infrastructure.Payments.Services;

/// <summary>Abstraction-only placeholder, per instructions — see DevInvoicePdfGenerator's doc comment.</summary>
internal sealed class DevReceiptPdfGenerator : IReceiptPdfGenerator
{
    public Task<string> GenerateAsync(ReceiptPdfData data, CancellationToken cancellationToken) =>
        Task.FromResult($"receipts/{data.ReceiptNumber}.pdf");
}
