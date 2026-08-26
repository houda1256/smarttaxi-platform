using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Application.Support.Contracts;

namespace SmartTaxi.Application.Support.Queries.GetSupportTicketDetailsAdmin;

/// <summary>Admin-only — gated by support.tickets.read.all at the API layer, no ownership check needed here since any admin may view any ticket. Includes internal notes, via GetAllForAdminAsync.</summary>
public sealed class GetSupportTicketDetailsAdminQueryHandler : IQueryHandler<GetSupportTicketDetailsAdminQuery, Result<SupportTicketDetails>>
{
    private const string NotFoundError = "Ticket introuvable.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketMessageRepository _messageRepository;

    public GetSupportTicketDetailsAdminQueryHandler(ISupportTicketRepository ticketRepository, ISupportTicketMessageRepository messageRepository)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Result<SupportTicketDetails>> Handle(GetSupportTicketDetailsAdminQuery query, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(query.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result<SupportTicketDetails>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var messages = await _messageRepository.GetAllForAdminAsync(query.TicketId, cancellationToken);
        return Result<SupportTicketDetails>.Success(new SupportTicketDetails(ticket, messages));
    }
}
