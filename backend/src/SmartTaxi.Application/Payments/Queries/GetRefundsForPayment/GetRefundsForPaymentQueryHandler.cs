using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetRefundsForPayment;

public sealed class GetRefundsForPaymentQueryHandler : IQueryHandler<GetRefundsForPaymentQuery, IReadOnlyCollection<RefundRecord>>
{
    private readonly IRefundRecordRepository _refundRecordRepository;

    public GetRefundsForPaymentQueryHandler(IRefundRecordRepository refundRecordRepository)
    {
        _refundRecordRepository = refundRecordRepository;
    }

    public Task<IReadOnlyCollection<RefundRecord>> Handle(GetRefundsForPaymentQuery query, CancellationToken cancellationToken) =>
        _refundRecordRepository.GetForPaymentAsync(query.PaymentId, cancellationToken);
}
