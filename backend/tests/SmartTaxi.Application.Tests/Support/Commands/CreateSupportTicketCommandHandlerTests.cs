using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Commands.CreateSupportTicket;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class CreateSupportTicketCommandHandlerTests
{
    private readonly FakeSupportTicketRepository _ticketRepository = new();
    private readonly FakeSupportRelatedEntityValidator _relatedEntityValidator = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly CreateSupportTicketCommandHandler _handler;

    public CreateSupportTicketCommandHandlerTests()
    {
        _handler = new CreateSupportTicketCommandHandler(_ticketRepository, _relatedEntityValidator, _notificationDispatcher);
    }

    private static CreateSupportTicketCommand BuildCommand(Guid requesterUserId, SupportRelatedEntityType? type = null, Guid? id = null) =>
        new(requesterUserId, SupportTicketCategory.Billing, "Facture incorrecte", "Le montant ne correspond pas.", SupportTicketPriority.Medium, type, id);

    [Fact]
    public async Task Handle_ValidRequest_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();

        var result = await _handler.Handle(BuildCommand(requesterUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_WithInvalidRelatedEntity_ReturnsForbidden()
    {
        _relatedEntityValidator.AlwaysValid = false;
        var requesterUserId = Guid.NewGuid();

        var result = await _handler.Handle(
            BuildCommand(requesterUserId, SupportRelatedEntityType.Ride, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Empty(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_WithValidRelatedEntity_Succeeds()
    {
        _relatedEntityValidator.AlwaysValid = true;
        var requesterUserId = Guid.NewGuid();

        var result = await _handler.Handle(
            BuildCommand(requesterUserId, SupportRelatedEntityType.Ride, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
