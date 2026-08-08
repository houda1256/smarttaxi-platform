using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<Guid, Invoice> _invoicesById = new();

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        _invoicesById[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        Task.FromResult(_invoicesById.GetValueOrDefault(invoiceId));

    public Task<Invoice?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var invoice = _invoicesById.Values.FirstOrDefault(i => i.PaymentId == paymentId);
        return Task.FromResult(invoice);
    }

    public Task<bool> TryAttachPdfAsync(Guid invoiceId, string pdfStorageKey, CancellationToken cancellationToken)
    {
        if (!_invoicesById.TryGetValue(invoiceId, out var invoice))
        {
            return Task.FromResult(false);
        }

        invoice.AttachPdf(pdfStorageKey);
        return Task.FromResult(true);
    }
}
