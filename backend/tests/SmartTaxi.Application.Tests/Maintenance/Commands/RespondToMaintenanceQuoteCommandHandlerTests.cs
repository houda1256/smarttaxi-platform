using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceQuote;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class RespondToMaintenanceQuoteCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly RespondToMaintenanceQuoteCommandHandler _handler;

    public RespondToMaintenanceQuoteCommandHandlerTests()
    {
        _handler = new RespondToMaintenanceQuoteCommandHandler(_requestRepository, _notificationDispatcher);
    }

    private async Task<MaintenanceRequest> CreateRequestAtQuoteSubmittedAsync(Guid ownerId, Guid garageUserId)
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuoteSubmitted, garageUserId, null, 100m,
            null, null, null, DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_Accept_SetsQuoteAccepted()
    {
        var ownerId = Guid.NewGuid();
        var request = await CreateRequestAtQuoteSubmittedAsync(ownerId, Guid.NewGuid());

        var result = await _handler.Handle(new RespondToMaintenanceQuoteCommand(request.Id, ownerId, IsAccepted: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.QuoteAccepted, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_Reject_SetsQuoteRejectedDistinctFromRequestRejected()
    {
        var ownerId = Guid.NewGuid();
        var request = await CreateRequestAtQuoteSubmittedAsync(ownerId, Guid.NewGuid());

        var result = await _handler.Handle(new RespondToMaintenanceQuoteCommand(request.Id, ownerId, IsAccepted: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.QuoteRejected, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_AnotherOwner_ReturnsConflict()
    {
        var actualOwnerId = Guid.NewGuid();
        var request = await CreateRequestAtQuoteSubmittedAsync(actualOwnerId, Guid.NewGuid());

        var result = await _handler.Handle(
            new RespondToMaintenanceQuoteCommand(request.Id, Guid.NewGuid(), IsAccepted: true), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
