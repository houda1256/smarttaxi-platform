using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Support.Commands.AssignSupportTicket;
using SmartTaxi.Application.Support.Commands.CloseSupportTicket;
using SmartTaxi.Application.Support.Commands.MarkWaitingForCustomer;
using SmartTaxi.Application.Support.Commands.ReassignSupportTicket;
using SmartTaxi.Application.Support.Commands.ReopenSupportTicket;
using SmartTaxi.Application.Support.Commands.ResolveSupportTicket;
using SmartTaxi.Application.Support.Commands.StartSupportTicket;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class TicketLifecycleCommandHandlerTests
{
    private readonly FakeSupportTicketRepository _ticketRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();

    private async Task<Guid> CreateAdminAsync()
    {
        var user = User.Create(Email.Create($"admin-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Admin);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user.Id;
    }

    private async Task<SupportTicket> CreateTicketAsync(Guid requesterUserId)
    {
        var ticket = SupportTicket.Create(
            requesterUserId, SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, DateTime.UtcNow);
        await _ticketRepository.TryAddAsync(ticket, CancellationToken.None);
        return ticket;
    }

    [Fact]
    public async Task Assign_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());
        var handler = new AssignSupportTicketCommandHandler(_ticketRepository, _notificationDispatcher);

        var first = await handler.Handle(new AssignSupportTicketCommand(ticket.Id, Guid.NewGuid()), CancellationToken.None);
        var second = await handler.Handle(new AssignSupportTicketCommand(ticket.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task Reassign_ByAnyAdmin_Succeeds()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());
        var assignHandler = new AssignSupportTicketCommandHandler(_ticketRepository, _notificationDispatcher);
        await assignHandler.Handle(new AssignSupportTicketCommand(ticket.Id, Guid.NewGuid()), CancellationToken.None);

        var newAdminId = Guid.NewGuid();
        var reassignHandler = new ReassignSupportTicketCommandHandler(_ticketRepository);
        var result = await reassignHandler.Handle(new ReassignSupportTicketCommand(ticket.Id, newAdminId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _ticketRepository.GetByIdAsync(ticket.Id, CancellationToken.None);
        Assert.Equal(newAdminId, reloaded!.AssignedAdminUserId);
    }

    private async Task<(SupportTicket Ticket, Guid AdminId)> CreateInProgressTicketAsync()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());
        var adminId = Guid.NewGuid();
        await _ticketRepository.TryAssignAsync(ticket.Id, adminId, DateTime.UtcNow, CancellationToken.None);
        await new StartSupportTicketCommandHandler(_ticketRepository).Handle(new StartSupportTicketCommand(ticket.Id, adminId), CancellationToken.None);
        return (ticket, adminId);
    }

    [Fact]
    public async Task Start_ByUnassignedAdmin_ReturnsConflict()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());
        await _ticketRepository.TryAssignAsync(ticket.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        var handler = new StartSupportTicketCommandHandler(_ticketRepository);
        var result = await handler.Handle(new StartSupportTicketCommand(ticket.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Resolve_ByAssignedAdmin_Succeeds()
    {
        var (ticket, adminId) = await CreateInProgressTicketAsync();
        var handler = new ResolveSupportTicketCommandHandler(_ticketRepository, _notificationDispatcher);

        var result = await handler.Handle(new ResolveSupportTicketCommand(ticket.Id, adminId, "Problème résolu"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _ticketRepository.GetByIdAsync(ticket.Id, CancellationToken.None);
        Assert.Equal(SupportTicketStatus.Resolved, reloaded!.Status);
        Assert.Equal("Problème résolu", reloaded.Resolution);
    }

    [Fact]
    public async Task Resolve_ByAnotherAdmin_ReturnsConflict()
    {
        var (ticket, _) = await CreateInProgressTicketAsync();
        var handler = new ResolveSupportTicketCommandHandler(_ticketRepository, _notificationDispatcher);

        var result = await handler.Handle(new ResolveSupportTicketCommand(ticket.Id, Guid.NewGuid(), "Tentative non autorisée"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Resolve_WithoutResolution_ReturnsValidationError()
    {
        var (ticket, adminId) = await CreateInProgressTicketAsync();
        var handler = new ResolveSupportTicketCommandHandler(_ticketRepository, _notificationDispatcher);

        var result = await handler.Handle(new ResolveSupportTicketCommand(ticket.Id, adminId, ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task MarkWaitingForCustomer_ByAssignedAdmin_Succeeds()
    {
        var (ticket, adminId) = await CreateInProgressTicketAsync();
        var handler = new MarkWaitingForCustomerCommandHandler(_ticketRepository, _notificationDispatcher);

        var result = await handler.Handle(new MarkWaitingForCustomerCommand(ticket.Id, adminId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private async Task<(SupportTicket Ticket, Guid AdminId)> CreateResolvedTicketAsync(Guid requesterUserId)
    {
        var ticket = await CreateTicketAsync(requesterUserId);
        var admin = Guid.NewGuid();
        await _ticketRepository.TryAssignAsync(ticket.Id, admin, DateTime.UtcNow, CancellationToken.None);
        await new StartSupportTicketCommandHandler(_ticketRepository).Handle(new StartSupportTicketCommand(ticket.Id, admin), CancellationToken.None);
        await new ResolveSupportTicketCommandHandler(_ticketRepository, _notificationDispatcher).Handle(
            new ResolveSupportTicketCommand(ticket.Id, admin, "Résolu"), CancellationToken.None);
        return (ticket, admin);
    }

    [Fact]
    public async Task Close_ByRequester_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();
        var (ticket, _) = await CreateResolvedTicketAsync(requesterUserId);
        var handler = new CloseSupportTicketCommandHandler(_ticketRepository, _userRepository);

        var result = await handler.Handle(new CloseSupportTicketCommand(ticket.Id, requesterUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Close_ByAdmin_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();
        var (ticket, _) = await CreateResolvedTicketAsync(requesterUserId);
        var adminUserId = await CreateAdminAsync();
        var handler = new CloseSupportTicketCommandHandler(_ticketRepository, _userRepository);

        var result = await handler.Handle(new CloseSupportTicketCommand(ticket.Id, adminUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Close_ByUnrelatedNonAdminUser_ReturnsForbidden()
    {
        var requesterUserId = Guid.NewGuid();
        var (ticket, _) = await CreateResolvedTicketAsync(requesterUserId);
        var handler = new CloseSupportTicketCommandHandler(_ticketRepository, _userRepository);

        var result = await handler.Handle(new CloseSupportTicketCommand(ticket.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Reopen_ByRequester_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();
        var (ticket, _) = await CreateResolvedTicketAsync(requesterUserId);
        var handler = new ReopenSupportTicketCommandHandler(_ticketRepository, _userRepository, _notificationDispatcher);

        var result = await handler.Handle(new ReopenSupportTicketCommand(ticket.Id, requesterUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _ticketRepository.GetByIdAsync(ticket.Id, CancellationToken.None);
        Assert.Equal(SupportTicketStatus.Reopened, reloaded!.Status);
    }

    [Fact]
    public async Task Reopen_ByUnrelatedNonAdminUser_ReturnsForbidden()
    {
        var requesterUserId = Guid.NewGuid();
        var (ticket, _) = await CreateResolvedTicketAsync(requesterUserId);
        var handler = new ReopenSupportTicketCommandHandler(_ticketRepository, _userRepository, _notificationDispatcher);

        var result = await handler.Handle(new ReopenSupportTicketCommand(ticket.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Resume_AfterReopen_ByAssignedAdmin_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();
        var (ticket, adminId) = await CreateResolvedTicketAsync(requesterUserId);
        await new ReopenSupportTicketCommandHandler(_ticketRepository, _userRepository, _notificationDispatcher).Handle(
            new ReopenSupportTicketCommand(ticket.Id, requesterUserId), CancellationToken.None);

        var handler = new StartSupportTicketCommandHandler(_ticketRepository);
        var result = await handler.Handle(new StartSupportTicketCommand(ticket.Id, adminId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _ticketRepository.GetByIdAsync(ticket.Id, CancellationToken.None);
        Assert.Equal(SupportTicketStatus.InProgress, reloaded!.Status);
    }
}
