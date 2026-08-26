using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Application.Support.Contracts;

namespace SmartTaxi.Application.Support.Queries.GetSupportTicketDetails;

/// <summary>Requester-only, own ticket. Messages are fetched via GetVisibleForRequesterAsync — internal notes are structurally never selected, not filtered after the fact.</summary>
public sealed class GetSupportTicketDetailsQueryHandler : IQueryHandler<GetSupportTicketDetailsQuery, Result<SupportTicketDetails>>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string ForbiddenError = "Vous n'avez pas accès à ce ticket.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketMessageRepository _messageRepository;

    public GetSupportTicketDetailsQueryHandler(ISupportTicketRepository ticketRepository, ISupportTicketMessageRepository messageRepository)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Result<SupportTicketDetails>> Handle(GetSupportTicketDetailsQuery query, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(query.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result<SupportTicketDetails>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ticket.RequesterUserId != query.RequestingUserId)
        {
            return Result<SupportTicketDetails>.Failure(ForbiddenError, ErrorType.Forbidden);
        }

        var messages = await _messageRepository.GetVisibleForRequesterAsync(query.TicketId, cancellationToken);
        return Result<SupportTicketDetails>.Success(new SupportTicketDetails(ticket, messages));
    }
}
