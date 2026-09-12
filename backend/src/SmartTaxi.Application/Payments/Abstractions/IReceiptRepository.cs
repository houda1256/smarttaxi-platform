using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Abstractions;

public interface IReceiptRepository
{
    Task AddAsync(Receipt receipt, CancellationToken cancellationToken);

    Task<Receipt?> GetByIdAsync(Guid receiptId, CancellationToken cancellationToken);

    Task<Receipt?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<bool> TryAttachPdfAsync(Guid receiptId, string pdfStorageKey, CancellationToken cancellationToken);
}
