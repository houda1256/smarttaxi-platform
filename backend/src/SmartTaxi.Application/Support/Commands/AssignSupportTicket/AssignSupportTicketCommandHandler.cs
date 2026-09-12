using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Support.Commands.AssignSupportTicket;

/// <summary>The atomic TryAssignAsync guard bakes "Status==Open AND AssignedAdminUserId IS NULL" into one statement — two admins claiming the same unassigned ticket resolve to exactly one winner.</summary>
public sealed class AssignSupportTicketCommandHandler : ICommandHandler<AssignSupportTicketCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string NotEligibleError = "Ce ticket n'est plus disponible pour une première affectation.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public AssignSupportTicketCommandHandler(ISupportTicketRepository ticketRepository, INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(AssignSupportTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var assigned = await _ticketRepository.TryAssignAsync(command.TicketId, command.AdminUserId, DateTime.UtcNow, cancellationToken);

        if (!assigned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                ticket.RequesterUserId, NotificationCategory.Support, "support.ticket.assigned", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
            cancellationToken);

        return Result.Success();
    }
}
