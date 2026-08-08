using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Abstractions;

public interface IPaymentTransactionHistoryRepository
{
    Task<IReadOnlyCollection<PaymentTransactionHistory>> GetForPaymentAsync(Guid paymentId, CancellationToken cancellationToken);
}
