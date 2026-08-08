using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetInvoiceByPaymentId;

public sealed class GetInvoiceByPaymentIdQueryHandler : IQueryHandler<GetInvoiceByPaymentIdQuery, Result<Invoice>>
{
    private const string NotFoundError = "Facture introuvable pour ce paiement.";

    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceByPaymentIdQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<Invoice>> Handle(GetInvoiceByPaymentIdQuery query, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByPaymentIdAsync(query.PaymentId, cancellationToken);

        return invoice is null ? Result<Invoice>.Failure(NotFoundError, ErrorType.NotFound) : Result<Invoice>.Success(invoice);
    }
}
