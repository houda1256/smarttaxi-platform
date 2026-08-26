using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Commands.AddAdminTicketMessage;
using SmartTaxi.Application.Support.Commands.AddInternalNote;
using SmartTaxi.Application.Support.Commands.AddTicketMessage;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class TicketMessageCommandHandlerTests
{
    private readonly FakeSupportTicketRepository _ticketRepository = new();
    private readonly FakeSupportTicketMessageRepository _messageRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();

    private SupportTicket CreateTicketWithRequester(Guid requesterUserId)
    {
        var ticket = SupportTicket.Create(
            requesterUserId, SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, DateTime.UtcNow);
        _ticketRepository.TryAddAsync(ticket, CancellationToken.None).Wait();
        return ticket;
    }

    [Fact]
    public async Task RequesterMessage_OnOwnTicket_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = CreateTicketWithRequester(requesterUserId);
        var handler = new AddTicketMessageCommandHandler(_ticketRepository, _notificationDispatcher);

        var result = await handler.Handle(new AddTicketMessageCommand(ticket.Id, requesterUserId, "Bonjour"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_ticketRepository.Messages);
        Assert.False(_ticketRepository.Messages[0].IsInternalNote);
    }

    [Fact]
    public async Task RequesterMessage_WhileWaitingForCustomer_AutomaticallyAdvancesToInProgress()
    {
        var requesterUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var ticket = CreateTicketWithRequester(requesterUserId);
        await _ticketRepository.TryAssignAsync(ticket.Id, adminUserId, DateTime.UtcNow, CancellationToken.None);
        await _ticketRepository.TryTransitionAsync(
            ticket.Id, [SupportTicketStatus.Assigned], SupportTicketStatus.InProgress, adminUserId, null, DateTime.UtcNow, CancellationToken.None);
        await _ticketRepository.TryTransitionAsync(
            ticket.Id, [SupportTicketStatus.InProgress], SupportTicketStatus.WaitingForCustomer, adminUserId, null, DateTime.UtcNow,
            CancellationToken.None);

        var handler = new AddTicketMessageCommandHandler(_ticketRepository, _notificationDispatcher);
        var result = await handler.Handle(new AddTicketMessageCommand(ticket.Id, requesterUserId, "Voici la précision demandée"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _ticketRepository.GetByIdAsync(ticket.Id, CancellationToken.None);
        Assert.Equal(SupportTicketStatus.InProgress, reloaded!.Status);
    }

    [Fact]
    public async Task RequesterMessage_OnClosedTicket_ReturnsConflict()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = CreateTicketWithRequester(requesterUserId);
        await _ticketRepository.TryTransitionAsync(
            ticket.Id, [SupportTicketStatus.Open], SupportTicketStatus.Resolved, null, "Résolu", DateTime.UtcNow, CancellationToken.None);
        await _ticketRepository.TryTransitionAsync(
            ticket.Id, [SupportTicketStatus.Resolved], SupportTicketStatus.Closed, null, null, DateTime.UtcNow, CancellationToken.None);

        var handler = new AddTicketMessageCommandHandler(_ticketRepository, _notificationDispatcher);
        var result = await handler.Handle(new AddTicketMessageCommand(ticket.Id, requesterUserId, "Toujours un souci"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task RequesterMessage_ByAnotherUser_ReturnsConflict()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = CreateTicketWithRequester(requesterUserId);

        var handler = new AddTicketMessageCommandHandler(_ticketRepository, _notificationDispatcher);
        var result = await handler.Handle(new AddTicketMessageCommand(ticket.Id, Guid.NewGuid(), "Intrusion"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AdminMessage_IsNeverInternal()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = CreateTicketWithRequester(requesterUserId);
        var handler = new AddAdminTicketMessageCommandHandler(_ticketRepository, _messageRepository, _notificationDispatcher);

        var result = await handler.Handle(new AddAdminTicketMessageCommand(ticket.Id, Guid.NewGuid(), "Réponse du support"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var visible = await _messageRepository.GetVisibleForRequesterAsync(ticket.Id, CancellationToken.None);
        Assert.Single(visible);
    }

    [Fact]
    public async Task InternalNote_IsNeverVisibleToRequester()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = CreateTicketWithRequester(requesterUserId);
        var handler = new AddInternalNoteCommandHandler(_ticketRepository, _messageRepository);

        var result = await handler.Handle(new AddInternalNoteCommand(ticket.Id, Guid.NewGuid(), "Client difficile, escalader si besoin"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var visible = await _messageRepository.GetVisibleForRequesterAsync(ticket.Id, CancellationToken.None);
        Assert.Empty(visible);
        var all = await _messageRepository.GetAllForAdminAsync(ticket.Id, CancellationToken.None);
        Assert.Single(all);
        Assert.True(all.Single().IsInternalNote);
        Assert.Empty(_notificationDispatcher.DispatchedRequests);
    }
}
