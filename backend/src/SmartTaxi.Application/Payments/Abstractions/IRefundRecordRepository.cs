using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Abstractions;

public interface IRefundRecordRepository
{
    Task AddAsync(RefundRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RefundRecord>> GetForPaymentAsync(Guid paymentId, CancellationToken cancellationToken);
}
