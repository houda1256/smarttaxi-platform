using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetMyPaymentsForCustomer;

public sealed class GetMyPaymentsForCustomerQueryHandler : IQueryHandler<GetMyPaymentsForCustomerQuery, IReadOnlyCollection<PaymentSummary>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetMyPaymentsForCustomerQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IReadOnlyCollection<PaymentSummary>> Handle(GetMyPaymentsForCustomerQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetForCustomerAsync(query.CustomerId, cancellationToken);
        return payments.Select(PaymentSummary.FromEntity).ToList();
    }
}
