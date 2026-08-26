using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Commands.AddInternalNote;

/// <summary>Never dispatches a notification to the requester — internal notes must never be surfaced to them, not even as a "you have a new response" hint.</summary>
public sealed class AddInternalNoteCommandHandler : ICommandHandler<AddInternalNoteCommand, Result>
{
    private const string NotFoundError = "Ticket introuvable.";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketMessageRepository _messageRepository;

    public AddInternalNoteCommandHandler(ISupportTicketRepository ticketRepository, ISupportTicketMessageRepository messageRepository)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Result> Handle(AddInternalNoteCommand command, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var note = SupportTicketMessage.Create(command.TicketId, command.AdminUserId, command.Body, isInternalNote: true, DateTime.UtcNow);
        await _messageRepository.AddAsync(note, cancellationToken);

        return Result.Success();
    }
}
