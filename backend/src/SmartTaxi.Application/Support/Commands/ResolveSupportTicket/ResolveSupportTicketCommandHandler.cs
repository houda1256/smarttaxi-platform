using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.ResolveSupportTicket;

/// <summary>InProgress -&gt; Resolved. Only the assigned admin may resolve — prevents an uninvolved admin closing out someone else's active case.</summary>
public sealed class ResolveSupportTicketCommandHandler : ICommandHandler<ResolveSupportTicketCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";
    private const string NotEligibleError = "Ce ticket n'est pas en cours de traitement.";
    private const string ResolutionRequiredError = "Une résolution est requise.";

    private static readonly SupportTicketStatus[] AllowedFromStatuses = [SupportTicketStatus.InProgress];

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ResolveSupportTicketCommandHandler(ISupportTicketRepository ticketRepository, INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ResolveSupportTicketCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Resolution))
        {
            return Result.Failure(ResolutionRequiredError, ErrorType.Validation);
        }

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _ticketRepository.TryTransitionAsync(
            command.TicketId, AllowedFromStatuses, SupportTicketStatus.Resolved, command.AdminUserId, command.Resolution, DateTime.UtcNow,
            cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                ticket.RequesterUserId, NotificationCategory.Support, "support.ticket.resolved", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
            cancellationToken);

        return Result.Success();
    }
}
