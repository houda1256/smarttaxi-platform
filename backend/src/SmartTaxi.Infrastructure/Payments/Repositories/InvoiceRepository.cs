using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _context;

    public InvoiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        _context.Invoices.FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);

    public Task<Invoice?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        _context.Invoices.FirstOrDefaultAsync(invoice => invoice.PaymentId == paymentId, cancellationToken);

    public async Task<bool> TryAttachPdfAsync(Guid invoiceId, string pdfStorageKey, CancellationToken cancellationToken)
    {
        var rows = await _context.Invoices
            .Where(invoice => invoice.Id == invoiceId && invoice.PdfStorageKey == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(invoice => invoice.PdfStorageKey, pdfStorageKey), cancellationToken);

        return rows == 1;
    }
}
