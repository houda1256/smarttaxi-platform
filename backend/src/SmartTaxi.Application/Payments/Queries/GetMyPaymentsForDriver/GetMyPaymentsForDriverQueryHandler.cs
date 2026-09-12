using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetMyPaymentsForDriver;

public sealed class GetMyPaymentsForDriverQueryHandler : IQueryHandler<GetMyPaymentsForDriverQuery, IReadOnlyCollection<PaymentSummary>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetMyPaymentsForDriverQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IReadOnlyCollection<PaymentSummary>> Handle(GetMyPaymentsForDriverQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetForDriverAsync(query.DriverProfileId, cancellationToken);
        return payments.Select(PaymentSummary.FromEntity).ToList();
    }
}
