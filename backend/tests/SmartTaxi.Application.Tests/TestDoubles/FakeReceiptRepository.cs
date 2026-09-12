using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeReceiptRepository : IReceiptRepository
{
    private readonly Dictionary<Guid, Receipt> _receiptsById = new();

    public Task AddAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        _receiptsById[receipt.Id] = receipt;
        return Task.CompletedTask;
    }

    public Task<Receipt?> GetByIdAsync(Guid receiptId, CancellationToken cancellationToken) =>
        Task.FromResult(_receiptsById.GetValueOrDefault(receiptId));

    public Task<Receipt?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var receipt = _receiptsById.Values.FirstOrDefault(r => r.PaymentId == paymentId);
        return Task.FromResult(receipt);
    }

    public Task<bool> TryAttachPdfAsync(Guid receiptId, string pdfStorageKey, CancellationToken cancellationToken)
    {
        if (!_receiptsById.TryGetValue(receiptId, out var receipt))
        {
            return Task.FromResult(false);
        }

        receipt.AttachPdf(pdfStorageKey);
        return Task.FromResult(true);
    }
}
