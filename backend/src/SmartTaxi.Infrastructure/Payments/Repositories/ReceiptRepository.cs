using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class ReceiptRepository : IReceiptRepository
{
    private readonly ApplicationDbContext _context;

    public ReceiptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        await _context.Receipts.AddAsync(receipt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Receipt?> GetByIdAsync(Guid receiptId, CancellationToken cancellationToken) =>
        _context.Receipts.FirstOrDefaultAsync(receipt => receipt.Id == receiptId, cancellationToken);

    public Task<Receipt?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        _context.Receipts.FirstOrDefaultAsync(receipt => receipt.PaymentId == paymentId, cancellationToken);

    public async Task<bool> TryAttachPdfAsync(Guid receiptId, string pdfStorageKey, CancellationToken cancellationToken)
    {
        var rows = await _context.Receipts
            .Where(receipt => receipt.Id == receiptId && receipt.PdfStorageKey == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(receipt => receipt.PdfStorageKey, pdfStorageKey), cancellationToken);

        return rows == 1;
    }
}
