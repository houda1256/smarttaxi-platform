using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Commands.CreateSupportTicket;

/// <summary>RelatedEntityId ownership is re-checked server-side via ISupportRelatedEntityValidator — never trusted from the request body, even though the caller's own identity comes from the JWT at the API layer.</summary>
public sealed class CreateSupportTicketCommandHandler : ICommandHandler<CreateSupportTicketCommand, Result<Guid>>
{
    private const string InvalidRelatedEntityError = "L'entité liée est introuvable ou ne vous appartient pas.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportRelatedEntityValidator _relatedEntityValidator;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CreateSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository, ISupportRelatedEntityValidator relatedEntityValidator,
        INotificationDispatcher notificationDispatcher)
    {
        _ticketRepository = ticketRepository;
        _relatedEntityValidator = relatedEntityValidator;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<Guid>> Handle(CreateSupportTicketCommand command, CancellationToken cancellationToken)
    {
        if (command.RelatedEntityType is { } relatedEntityType && command.RelatedEntityId is { } relatedEntityId)
        {
            var isValid = await _relatedEntityValidator.IsValidReferenceAsync(
                relatedEntityType, relatedEntityId, command.RequesterUserId, cancellationToken);

            if (!isValid)
            {
                return Result<Guid>.Failure(InvalidRelatedEntityError, ErrorType.Forbidden);
            }
        }

        SupportTicket ticket;

        try
        {
            ticket = SupportTicket.Create(
                command.RequesterUserId, command.Category, command.Subject, command.Description, command.Priority,
                command.RelatedEntityType, command.RelatedEntityId, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _ticketRepository.TryAddAsync(ticket, cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                ticket.RequesterUserId, NotificationCategory.Support, "support.ticket.created", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "SupportTicket", SourceId: ticket.Id),
            cancellationToken);

        return Result<Guid>.Success(ticket.Id);
    }
}
