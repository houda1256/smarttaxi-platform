using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceById;

public sealed class GetGroupedInvoiceByIdQueryHandler : IQueryHandler<GetGroupedInvoiceByIdQuery, Result<GroupedInvoice>>
{
    private const string NotFoundError = "Facture groupée introuvable.";

    private readonly IGroupedInvoiceRepository _invoiceRepository;

    public GetGroupedInvoiceByIdQueryHandler(IGroupedInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<GroupedInvoice>> Handle(GetGroupedInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(query.GroupedInvoiceId, cancellationToken);

        return invoice is null
            ? Result<GroupedInvoice>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<GroupedInvoice>.Success(invoice);
    }
}
