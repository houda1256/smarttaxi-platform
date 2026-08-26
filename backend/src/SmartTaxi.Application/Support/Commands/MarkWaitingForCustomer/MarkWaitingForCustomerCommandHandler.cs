using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.MarkWaitingForCustomer;

/// <summary>InProgress -&gt; WaitingForCustomer. Explicit admin action only — never automatic (only the reverse transition is automatic, on a requester reply, per the approved design).</summary>
public sealed class MarkWaitingForCustomerCommandHandler : ICommandHandler<MarkWaitingForCustomerCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string NotEligibleError = "Ce ticket n'est pas dans un état permettant ce changement.";

    private static readonly SupportTicketStatus[] AllowedFromStatuses = [SupportTicketStatus.InProgress];

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public MarkWaitingForCustomerCommandHandler(ISupportTicketRepository ticketRepository, INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(MarkWaitingForCustomerCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _ticketRepository.TryTransitionAsync(
            command.TicketId, AllowedFromStatuses, SupportTicketStatus.WaitingForCustomer, command.AdminUserId, resolution: null,
            DateTime.UtcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                ticket.RequesterUserId, NotificationCategory.Support, "support.ticket.waiting-for-customer", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
            cancellationToken);

        return Result.Success();
    }
}
