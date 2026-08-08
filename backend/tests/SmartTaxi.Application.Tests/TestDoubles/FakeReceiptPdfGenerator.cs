using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeReceiptPdfGenerator : IReceiptPdfGenerator
{
    public Task<string> GenerateAsync(ReceiptPdfData data, CancellationToken cancellationToken) =>
        Task.FromResult($"receipts/{data.ReceiptNumber}.pdf");
}
