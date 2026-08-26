using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetMySupportTickets;

public sealed class GetMySupportTicketsQueryHandler : IQueryHandler<GetMySupportTicketsQuery, PagedResult<SupportTicket>>
{
    private readonly ISupportTicketRepository _repository;

    public GetMySupportTicketsQueryHandler(ISupportTicketRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<SupportTicket>> Handle(GetMySupportTicketsQuery query, CancellationToken cancellationToken) =>
        _repository.GetForRequesterAsync(query.RequesterUserId, query.PageNumber, query.PageSize, cancellationToken);
}
