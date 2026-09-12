using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceLines;

public sealed class GetGroupedInvoiceLinesQueryHandler : IQueryHandler<GetGroupedInvoiceLinesQuery, IReadOnlyCollection<GroupedInvoiceLine>>
{
    private readonly IGroupedInvoiceLineRepository _lineRepository;

    public GetGroupedInvoiceLinesQueryHandler(IGroupedInvoiceLineRepository lineRepository)
    {
        _lineRepository = lineRepository;
    }

    public Task<IReadOnlyCollection<GroupedInvoiceLine>> Handle(GetGroupedInvoiceLinesQuery query, CancellationToken cancellationToken) =>
        _lineRepository.GetForInvoiceAsync(query.GroupedInvoiceId, cancellationToken);
}
