using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoicesForBusinessCustomer;

public sealed class GetGroupedInvoicesForBusinessCustomerQueryHandler : IQueryHandler<GetGroupedInvoicesForBusinessCustomerQuery, PagedResult<GroupedInvoice>>
{
    private readonly IGroupedInvoiceRepository _invoiceRepository;

    public GetGroupedInvoicesForBusinessCustomerQueryHandler(IGroupedInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public Task<PagedResult<GroupedInvoice>> Handle(GetGroupedInvoicesForBusinessCustomerQuery query, CancellationToken cancellationToken) =>
        _invoiceRepository.GetForBusinessCustomerAsync(query.BusinessCustomerId, query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
