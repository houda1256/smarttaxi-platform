using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CloseSupportTicket;

/// <summary>
/// Resolved -&gt; Closed. Either the requester (own ticket) or any admin may
/// close — this single command serves both /api/support/tickets/{id}/close
/// and /api/admin/support/tickets/{id}/close, since the actual authorization
/// rule is identical (own-ticket-or-admin), never a client-supplied "is
/// admin" flag: the caller's role is re-derived server-side via
/// IUserRepository. Since both authorized actors are simply allowed (no
/// "wrong actor beats right actor" race to prevent, unlike Resolve), the
/// atomic transition itself needs no actor parameter — only the status race
/// matters at the DB level.
/// </summary>
public sealed class CloseSupportTicketCommandHandler : ICommandHandler<CloseSupportTicketCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string ForbiddenError = "Vous ne pouvez pas fermer ce ticket.";
    private const string NotEligibleError = "Ce ticket n'est pas résolu.";

    private static readonly SupportTicketStatus[] AllowedFromStatuses = [SupportTicketStatus.Resolved];

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public CloseSupportTicketCommandHandler(ISupportTicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(CloseSupportTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ticket.RequesterUserId != command.CallerUserId)
        {
            var caller = await _userRepository.GetByIdAsync(command.CallerUserId, cancellationToken);

            if (caller is null || !caller.HasRole(UserRole.Admin))
            {
                return Result.Failure(ForbiddenError, ErrorType.Forbidden);
            }
        }

        var transitioned = await _ticketRepository.TryTransitionAsync(
            command.TicketId, AllowedFromStatuses, SupportTicketStatus.Closed, requiredAdminUserId: null, resolution: null, DateTime.UtcNow,
            cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}
