using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetReceiptByPaymentId;

public sealed class GetReceiptByPaymentIdQueryHandler : IQueryHandler<GetReceiptByPaymentIdQuery, Result<Receipt>>
{
    private const string NotFoundError = "Reçu introuvable pour ce paiement.";

    private readonly IReceiptRepository _receiptRepository;

    public GetReceiptByPaymentIdQueryHandler(IReceiptRepository receiptRepository)
    {
        _receiptRepository = receiptRepository;
    }

    public async Task<Result<Receipt>> Handle(GetReceiptByPaymentIdQuery query, CancellationToken cancellationToken)
    {
        var receipt = await _receiptRepository.GetByPaymentIdAsync(query.PaymentId, cancellationToken);

        return receipt is null ? Result<Receipt>.Failure(NotFoundError, ErrorType.NotFound) : Result<Receipt>.Success(receipt);
    }
}
