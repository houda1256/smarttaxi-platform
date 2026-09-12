using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class RespondToMaintenanceRequestCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly RespondToMaintenanceRequestCommandHandler _handler;

    public RespondToMaintenanceRequestCommandHandlerTests()
    {
        _handler = new RespondToMaintenanceRequestCommandHandler(_requestRepository, _notificationDispatcher);
    }

    private async Task<MaintenanceRequest> CreateRequestAsync(Guid ownerId, Guid garageUserId)
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_Accept_CollapsesDirectlyToQuotePending()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(Guid.NewGuid(), garageUserId);

        var result = await _handler.Handle(
            new RespondToMaintenanceRequestCommand(request.Id, garageUserId, IsAccepted: true, RejectionReason: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.QuotePending, reloaded!.Status);
        Assert.NotNull(reloaded.ConfirmedAtUtc);
    }

    [Fact]
    public async Task Handle_Reject_SetsRejectedStatusDistinctFromQuoteRejectedAndCancelled()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(Guid.NewGuid(), garageUserId);

        var result = await _handler.Handle(
            new RespondToMaintenanceRequestCommand(request.Id, garageUserId, IsAccepted: false, RejectionReason: "Pas disponible"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Rejected, reloaded!.Status);
        Assert.Equal("Pas disponible", reloaded.GarageRejectionReason);
    }

    [Fact]
    public async Task Handle_Reject_WithoutReason_ReturnsValidationError()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(Guid.NewGuid(), garageUserId);

        var result = await _handler.Handle(
            new RespondToMaintenanceRequestCommand(request.Id, garageUserId, IsAccepted: false, RejectionReason: null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AnotherGarageRespondingToThisRequest_ReturnsConflict()
    {
        var actualGarageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(Guid.NewGuid(), actualGarageUserId);
        var impersonatingGarageUserId = Guid.NewGuid();

        var result = await _handler.Handle(
            new RespondToMaintenanceRequestCommand(request.Id, impersonatingGarageUserId, IsAccepted: true, RejectionReason: null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.PendingGarageResponse, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_AlreadyRespondedTo_ReturnsConflictOnSecondAttempt()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(Guid.NewGuid(), garageUserId);

        var first = await _handler.Handle(
            new RespondToMaintenanceRequestCommand(request.Id, garageUserId, IsAccepted: true, RejectionReason: null), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(
            new RespondToMaintenanceRequestCommand(request.Id, garageUserId, IsAccepted: true, RejectionReason: null), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }
}
