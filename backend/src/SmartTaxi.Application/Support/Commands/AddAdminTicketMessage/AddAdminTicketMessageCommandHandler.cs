using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Commands.AddAdminTicketMessage;

/// <summary>Requester-visible response — never an internal note (see AddInternalNoteCommand for that). A plain insert: unlike the requester's own message path, an admin response never changes ticket status automatically.</summary>
public sealed class AddAdminTicketMessageCommandHandler : ICommandHandler<AddAdminTicketMessageCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketMessageRepository _messageRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public AddAdminTicketMessageCommandHandler(
        ISupportTicketRepository ticketRepository, ISupportTicketMessageRepository messageRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(AddAdminTicketMessageCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var message = SupportTicketMessage.Create(command.TicketId, command.AdminUserId, command.Body, isInternalNote: false, DateTime.UtcNow);
        await _messageRepository.AddAsync(message, cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                ticket.RequesterUserId, NotificationCategory.Support, "support.ticket.responded", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
            cancellationToken);

        return Result.Success();
    }
}
