using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler : IQueryHandler<GetPaymentByIdQuery, Result<PaymentSummary>>
{
    private const string NotFoundError = "Paiement introuvable.";
    private const string NotParticipantError = "Seuls les participants au paiement peuvent le consulter.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository)
    {
        _paymentRepository = paymentRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result<PaymentSummary>> Handle(GetPaymentByIdQuery query, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(query.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result<PaymentSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(payment.DriverId, cancellationToken);
        var isOwner = payment.OwnerId == query.RequestingUserId;

        if (query.RequestingUserId != payment.CustomerId && (driver is null || driver.UserId != query.RequestingUserId) && !isOwner)
        {
            return Result<PaymentSummary>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        return Result<PaymentSummary>.Success(PaymentSummary.FromEntity(payment));
    }
}
