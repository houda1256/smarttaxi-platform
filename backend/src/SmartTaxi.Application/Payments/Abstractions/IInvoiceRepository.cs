using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Abstractions;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<Invoice?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<bool> TryAttachPdfAsync(Guid invoiceId, string pdfStorageKey, CancellationToken cancellationToken);
}
