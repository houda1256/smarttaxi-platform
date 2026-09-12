using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.CancelMaintenanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class CancelMaintenanceRequestCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly CancelMaintenanceRequestCommandHandler _handler;

    public CancelMaintenanceRequestCommandHandlerTests()
    {
        _handler = new CancelMaintenanceRequestCommandHandler(_requestRepository, _notificationDispatcher);
    }

    private async Task<MaintenanceRequest> CreateRequestAsync(Guid ownerId, Guid garageUserId)
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_WhilePendingGarageResponse_OwnerCanCancel()
    {
        var ownerId = Guid.NewGuid();
        var request = await CreateRequestAsync(ownerId, Guid.NewGuid());

        var result = await _handler.Handle(new CancelMaintenanceRequestCommand(request.Id, ownerId, "Changement d'avis"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Cancelled, reloaded!.Status);
        Assert.Equal(ownerId, reloaded.CancelledByUserId);
    }

    [Fact]
    public async Task Handle_ByAnotherOwner_ReturnsConflict()
    {
        var actualOwnerId = Guid.NewGuid();
        var request = await CreateRequestAsync(actualOwnerId, Guid.NewGuid());
        var impersonatorId = Guid.NewGuid();

        var result = await _handler.Handle(new CancelMaintenanceRequestCommand(request.Id, impersonatorId, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AfterVehicleReceived_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(ownerId, garageUserId);

        // Drive the request past VehicleReceived — cancellation must no longer be legal.
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuotePending, garageUserId, null, null, null,
            null, null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.QuotePending], MaintenanceRequestStatus.QuoteSubmitted, garageUserId, null, 100m, null, null,
            null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.QuoteSubmitted], MaintenanceRequestStatus.QuoteAccepted, null, ownerId, null, null, null, null,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.QuoteAccepted], MaintenanceRequestStatus.VehicleReceived, garageUserId, null, null, null, null,
            null, DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new CancelMaintenanceRequestCommand(request.Id, ownerId, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
