using SmartTaxi.Application.Maintenance.Commands.MarkWaitingForParts;
using SmartTaxi.Application.Maintenance.Commands.ResumeMaintenanceWork;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class MarkWaitingForPartsAndResumeCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly MarkWaitingForPartsCommandHandler _waitingHandler;
    private readonly ResumeMaintenanceWorkCommandHandler _resumeHandler;

    public MarkWaitingForPartsAndResumeCommandHandlerTests()
    {
        _waitingHandler = new MarkWaitingForPartsCommandHandler(_requestRepository);
        _resumeHandler = new ResumeMaintenanceWorkCommandHandler(_requestRepository);
    }

    private async Task<(MaintenanceRequest Request, Guid GarageUserId)> CreateRequestInProgressAsync()
    {
        var garageUserId = Guid.NewGuid();
        var request = MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.InProgress, garageUserId, null, null, null,
            null, null, DateTime.UtcNow, CancellationToken.None);
        return (request, garageUserId);
    }

    [Fact]
    public async Task Handle_MarkWaitingForParts_ThenResume_RoundTripsWithoutTouchingWorkStartedAtUtc()
    {
        var (request, garageUserId) = await CreateRequestInProgressAsync();
        var beforeWaiting = (await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None))!.WorkStartedAtUtc;

        var waitingResult = await _waitingHandler.Handle(new MarkWaitingForPartsCommand(request.Id, garageUserId), CancellationToken.None);
        Assert.True(waitingResult.IsSuccess);
        Assert.Equal(MaintenanceRequestStatus.WaitingForParts, (await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None))!.Status);

        var resumeResult = await _resumeHandler.Handle(new ResumeMaintenanceWorkCommand(request.Id, garageUserId), CancellationToken.None);
        Assert.True(resumeResult.IsSuccess);

        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.InProgress, reloaded!.Status);
        Assert.Equal(beforeWaiting, reloaded.WorkStartedAtUtc); // resuming never re-stamps the original work-start timestamp
    }
}
