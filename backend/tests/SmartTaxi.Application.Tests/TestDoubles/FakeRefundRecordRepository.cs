using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRefundRecordRepository : IRefundRecordRepository
{
    private readonly List<RefundRecord> _records = [];

    public Task AddAsync(RefundRecord record, CancellationToken cancellationToken)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RefundRecord>> GetForPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RefundRecord> records = _records.Where(r => r.PaymentId == paymentId).ToList();
        return Task.FromResult(records);
    }
}
