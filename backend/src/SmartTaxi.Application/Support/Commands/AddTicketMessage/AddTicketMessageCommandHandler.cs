using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Support.Commands.AddTicketMessage;

/// <summary>
/// Requester-only path — the atomic TryAddRequesterMessageAndAdvanceAsync
/// guard bakes RequesterUserId==caller into the same statement as the
/// Status!=Closed guard, and folds the approved WaitingForCustomer-&gt;InProgress
/// automatic transition into the same transaction as the message insert
/// (never two independently committed writes).
/// </summary>
public sealed class AddTicketMessageCommandHandler : ICommandHandler<AddTicketMessageCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string NotEligibleError = "Ce ticket est fermé — veuillez le rouvrir avant d'ajouter un message.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public AddTicketMessageCommandHandler(ISupportTicketRepository ticketRepository, INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(AddTicketMessageCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        var added = await _ticketRepository.TryAddRequesterMessageAndAdvanceAsync(
            command.TicketId, command.RequesterUserId, command.Body, utcNow, cancellationToken);

        if (!added)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        if (ticket.AssignedAdminUserId is { } adminUserId)
        {
            await _notificationDispatcher.DispatchAsync(
                new NotificationRequest(
                    adminUserId, NotificationCategory.Support, "support.ticket.responded", new Dictionary<string, string>(),
                    IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
                cancellationToken);
        }

        return Result.Success();
    }
}
