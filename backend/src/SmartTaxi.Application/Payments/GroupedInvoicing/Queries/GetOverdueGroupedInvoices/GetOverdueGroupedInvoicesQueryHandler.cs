using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetOverdueGroupedInvoices;

public sealed class GetOverdueGroupedInvoicesQueryHandler : IQueryHandler<GetOverdueGroupedInvoicesQuery, IReadOnlyCollection<GroupedInvoice>>
{
    private readonly IGroupedInvoiceRepository _invoiceRepository;

    public GetOverdueGroupedInvoicesQueryHandler(IGroupedInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public Task<IReadOnlyCollection<GroupedInvoice>> Handle(GetOverdueGroupedInvoicesQuery query, CancellationToken cancellationToken) =>
        _invoiceRepository.GetOverdueAsync(query.AsOfUtc, cancellationToken);
}
