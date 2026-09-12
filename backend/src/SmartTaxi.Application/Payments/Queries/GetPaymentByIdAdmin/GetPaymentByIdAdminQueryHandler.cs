using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentByIdAdmin;

public sealed class GetPaymentByIdAdminQueryHandler : IQueryHandler<GetPaymentByIdAdminQuery, Result<PaymentSummary>>
{
    private const string NotFoundError = "Paiement introuvable.";

    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByIdAdminQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PaymentSummary>> Handle(GetPaymentByIdAdminQuery query, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(query.PaymentId, cancellationToken);

        return payment is null
            ? Result<PaymentSummary>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<PaymentSummary>.Success(PaymentSummary.FromEntity(payment));
    }
}
