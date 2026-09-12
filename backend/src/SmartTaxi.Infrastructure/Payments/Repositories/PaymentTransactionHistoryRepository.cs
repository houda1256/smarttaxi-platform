using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class PaymentTransactionHistoryRepository : IPaymentTransactionHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentTransactionHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<PaymentTransactionHistory>> GetForPaymentAsync(Guid paymentId, CancellationToken cancellationToken) =>
        await _context.PaymentTransactionHistories
            .Where(history => history.PaymentId == paymentId)
            .OrderBy(history => history.ChangedAt)
            .ToListAsync(cancellationToken);
}
