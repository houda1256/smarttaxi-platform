using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.ReopenSupportTicket;

/// <summary>Resolved/Closed -&gt; Reopened. Same own-ticket-or-admin rule as CloseSupportTicketCommand, serving both the requester and admin routes. No reopen-window enforcement in this MVP (Support.ReopenDelayDays is a deferred SystemSetting concern).</summary>
public sealed class ReopenSupportTicketCommandHandler : ICommandHandler<ReopenSupportTicketCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string ForbiddenError = "Vous ne pouvez pas rouvrir ce ticket.";
    private const string NotEligibleError = "Ce ticket ne peut pas être rouvert dans son état actuel.";

    private static readonly SupportTicketStatus[] AllowedFromStatuses = [SupportTicketStatus.Resolved, SupportTicketStatus.Closed];

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ReopenSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository, IUserRepository userRepository, INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ReopenSupportTicketCommand command, CancellationToken cancellationToken)
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
            command.TicketId, AllowedFromStatuses, SupportTicketStatus.Reopened, requiredAdminUserId: null, resolution: null, DateTime.UtcNow,
            cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        if (ticket.AssignedAdminUserId is { } adminUserId)
        {
            await _notificationDispatcher.DispatchAsync(
                new NotificationRequest(
                    adminUserId, NotificationCategory.Support, "support.ticket.reopened", new Dictionary<string, string>(),
                    IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
                cancellationToken);
        }

        return Result.Success();
    }
}
