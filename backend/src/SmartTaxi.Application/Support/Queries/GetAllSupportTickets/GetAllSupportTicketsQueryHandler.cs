using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetAllSupportTickets;

public sealed class GetAllSupportTicketsQueryHandler : IQueryHandler<GetAllSupportTicketsQuery, PagedResult<SupportTicket>>
{
    private readonly ISupportTicketRepository _repository;

    public GetAllSupportTicketsQueryHandler(ISupportTicketRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<SupportTicket>> Handle(GetAllSupportTicketsQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}
