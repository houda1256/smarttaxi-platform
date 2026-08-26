using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.StartSupportTicket;

/// <summary>Assigned -&gt; InProgress. Only the assigned admin — the atomic guard bakes AssignedAdminUserId==caller into the same statement as the status guard.</summary>
public sealed class StartSupportTicketCommandHandler : ICommandHandler<StartSupportTicketCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string NotEligibleError = "Ce ticket n'est pas dans un état permettant ce changement.";

    private static readonly SupportTicketStatus[] AllowedFromStatuses = [SupportTicketStatus.Assigned, SupportTicketStatus.Reopened];

    private readonly ISupportTicketRepository _ticketRepository;

    public StartSupportTicketCommandHandler(ISupportTicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Result> Handle(StartSupportTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _ticketRepository.TryTransitionAsync(
            command.TicketId, AllowedFromStatuses, SupportTicketStatus.InProgress, command.AdminUserId, resolution: null, DateTime.UtcNow,
            cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}
