using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Application.Support.Queries.GetAllSupportTickets;
using SmartTaxi.Application.Support.Queries.GetMySupportTickets;
using SmartTaxi.Application.Support.Queries.GetSupportTicketDetails;
using SmartTaxi.Application.Support.Queries.GetSupportTicketDetailsAdmin;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Queries;

public class SupportTicketQueryHandlerTests
{
    private readonly FakeSupportTicketRepository _ticketRepository = new();
    private readonly FakeSupportTicketMessageRepository _messageRepository = new();

    private async Task<SupportTicket> CreateTicketAsync(Guid requesterUserId)
    {
        var ticket = SupportTicket.Create(
            requesterUserId, SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, DateTime.UtcNow);
        await _ticketRepository.TryAddAsync(ticket, CancellationToken.None);
        return ticket;
    }

    [Fact]
    public async Task GetMyTickets_ReturnsOnlyOwnTickets()
    {
        var requesterUserId = Guid.NewGuid();
        await CreateTicketAsync(requesterUserId);
        await CreateTicketAsync(Guid.NewGuid());

        var handler = new GetMySupportTicketsQueryHandler(_ticketRepository);
        var result = await handler.Handle(new GetMySupportTicketsQuery(requesterUserId, 1, 10), CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryTicket()
    {
        await CreateTicketAsync(Guid.NewGuid());
        await CreateTicketAsync(Guid.NewGuid());

        var handler = new GetAllSupportTicketsQueryHandler(_ticketRepository);
        var result = await handler.Handle(new GetAllSupportTicketsQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetDetails_AsOwner_ExcludesInternalNotes()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = await CreateTicketAsync(requesterUserId);
        await _messageRepository.AddAsync(
            Domain.Support.Entities.SupportTicketMessage.Create(ticket.Id, requesterUserId, "Message visible", false, DateTime.UtcNow),
            CancellationToken.None);
        await _messageRepository.AddAsync(
            Domain.Support.Entities.SupportTicketMessage.Create(ticket.Id, Guid.NewGuid(), "Note interne", true, DateTime.UtcNow),
            CancellationToken.None);

        var handler = new GetSupportTicketDetailsQueryHandler(_ticketRepository, _messageRepository);
        var result = await handler.Handle(new GetSupportTicketDetailsQuery(ticket.Id, requesterUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Messages);
        Assert.False(result.Value!.Messages.Single().IsInternalNote);
    }

    [Fact]
    public async Task GetDetails_AsUnrelatedUser_ReturnsForbidden()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());

        var handler = new GetSupportTicketDetailsQueryHandler(_ticketRepository, _messageRepository);
        var result = await handler.Handle(new GetSupportTicketDetailsQuery(ticket.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetDetailsAdmin_IncludesInternalNotes()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = await CreateTicketAsync(requesterUserId);
        await _messageRepository.AddAsync(
            Domain.Support.Entities.SupportTicketMessage.Create(ticket.Id, Guid.NewGuid(), "Note interne", true, DateTime.UtcNow),
            CancellationToken.None);

        var handler = new GetSupportTicketDetailsAdminQueryHandler(_ticketRepository, _messageRepository);
        var result = await handler.Handle(new GetSupportTicketDetailsAdminQuery(ticket.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Messages);
        Assert.True(result.Value!.Messages.Single().IsInternalNote);
    }
}
