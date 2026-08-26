using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;

namespace SmartTaxi.Application.Support.Commands.ReassignSupportTicket;

/// <summary>Admin override — callable by any admin, at any non-Closed status, regardless of who currently holds the ticket.</summary>
public sealed class ReassignSupportTicketCommandHandler : ICommandHandler<ReassignSupportTicketCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string NotEligibleError = "Ce ticket est fermé et ne peut plus être réaffecté.";

    private readonly ISupportTicketRepository _ticketRepository;

    public ReassignSupportTicketCommandHandler(ISupportTicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Result> Handle(ReassignSupportTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var reassigned = await _ticketRepository.TryReassignAsync(command.TicketId, command.NewAdminUserId, DateTime.UtcNow, cancellationToken);

        return reassigned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}
