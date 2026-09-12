using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentsForOwner;

public sealed class GetPaymentsForOwnerQueryHandler : IQueryHandler<GetPaymentsForOwnerQuery, IReadOnlyCollection<PaymentSummary>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsForOwnerQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IReadOnlyCollection<PaymentSummary>> Handle(GetPaymentsForOwnerQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetForOwnerAsync(query.OwnerId, cancellationToken);
        return payments.Select(PaymentSummary.FromEntity).ToList();
    }
}
