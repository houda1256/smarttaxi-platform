using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class RefundRecordRepository : IRefundRecordRepository
{
    private readonly ApplicationDbContext _context;

    public RefundRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefundRecord record, CancellationToken cancellationToken)
    {
        await _context.RefundRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RefundRecord>> GetForPaymentAsync(Guid paymentId, CancellationToken cancellationToken) =>
        await _context.RefundRecords.Where(record => record.PaymentId == paymentId).ToListAsync(cancellationToken);
}
