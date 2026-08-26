using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.SubmitMaintenanceQuote;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class SubmitMaintenanceQuoteCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly SubmitMaintenanceQuoteCommandHandler _handler;

    public SubmitMaintenanceQuoteCommandHandlerTests()
    {
        _handler = new SubmitMaintenanceQuoteCommandHandler(_requestRepository, _notificationDispatcher);
    }

    private async Task<MaintenanceRequest> CreateRequestAtQuotePendingAsync(Guid garageUserId)
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuotePending, garageUserId, null, null, null,
            null, null, DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_ValidQuote_Succeeds()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAtQuotePendingAsync(garageUserId);

        var result = await _handler.Handle(new SubmitMaintenanceQuoteCommand(request.Id, garageUserId, 150m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.QuoteSubmitted, reloaded!.Status);
        Assert.Equal(150m, reloaded.EstimatedCost);
    }

    [Fact]
    public async Task Handle_NonPositiveCost_ReturnsValidationError()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAtQuotePendingAsync(garageUserId);

        var result = await _handler.Handle(new SubmitMaintenanceQuoteCommand(request.Id, garageUserId, 0m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AnotherGarage_ReturnsConflict()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAtQuotePendingAsync(garageUserId);

        var result = await _handler.Handle(new SubmitMaintenanceQuoteCommand(request.Id, Guid.NewGuid(), 150m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
