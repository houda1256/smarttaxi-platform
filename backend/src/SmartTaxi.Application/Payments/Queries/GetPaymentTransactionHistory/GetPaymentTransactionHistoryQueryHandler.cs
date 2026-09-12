using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentTransactionHistory;

public sealed class GetPaymentTransactionHistoryQueryHandler
    : IQueryHandler<GetPaymentTransactionHistoryQuery, IReadOnlyCollection<PaymentTransactionHistory>>
{
    private readonly IPaymentTransactionHistoryRepository _historyRepository;

    public GetPaymentTransactionHistoryQueryHandler(IPaymentTransactionHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task<IReadOnlyCollection<PaymentTransactionHistory>> Handle(
        GetPaymentTransactionHistoryQuery query, CancellationToken cancellationToken) =>
        _historyRepository.GetForPaymentAsync(query.PaymentId, cancellationToken);
}
