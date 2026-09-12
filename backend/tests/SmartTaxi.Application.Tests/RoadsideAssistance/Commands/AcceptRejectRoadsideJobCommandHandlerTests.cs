using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.AcceptRoadsideJob;
using SmartTaxi.Application.RoadsideAssistance.Commands.RejectRoadsideJob;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class AcceptRejectRoadsideJobCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly AcceptRoadsideJobCommandHandler _acceptHandler;
    private readonly RejectRoadsideJobCommandHandler _rejectHandler;

    public AcceptRejectRoadsideJobCommandHandlerTests()
    {
        _acceptHandler = new AcceptRoadsideJobCommandHandler(_requestRepository, _notificationDispatcher);
        _rejectHandler = new RejectRoadsideJobCommandHandler(_requestRepository, _notificationDispatcher);
    }

    private async Task<(RoadsideAssistanceRequest Request, Guid PartnerUserId)> CreatePendingResponseRequestAsync()
    {
        var requesterUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur", 36.8, 10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        return (request, partnerUserId);
    }

    [Fact]
    public async Task Accept_BySelectedPartner_Succeeds()
    {
        var (request, partnerUserId) = await CreatePendingResponseRequestAsync();

        var result = await _acceptHandler.Handle(new AcceptRoadsideJobCommand(request.Id, partnerUserId, 50m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Accepted, reloaded!.Status);
        Assert.Equal(50m, reloaded.EstimatedCost);
        var history = Assert.Single(_requestRepository.GetHistoryForTest(request.Id));
        Assert.Equal(RoadsidePartnerResponse.Accepted, history.Response);
    }

    [Fact]
    public async Task Accept_ByUnrelatedPartner_ReturnsConflict()
    {
        var (request, _) = await CreatePendingResponseRequestAsync();

        var result = await _acceptHandler.Handle(new AcceptRoadsideJobCommand(request.Id, Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Accept_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var (request, partnerUserId) = await CreatePendingResponseRequestAsync();

        var first = await _acceptHandler.Handle(new AcceptRoadsideJobCommand(request.Id, partnerUserId, null), CancellationToken.None);
        var second = await _acceptHandler.Handle(new AcceptRoadsideJobCommand(request.Id, partnerUserId, null), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
    }

    [Fact]
    public async Task Reject_BySelectedPartnerWithReason_Succeeds()
    {
        var (request, partnerUserId) = await CreatePendingResponseRequestAsync();

        var result = await _rejectHandler.Handle(new RejectRoadsideJobCommand(request.Id, partnerUserId, "Indisponible"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Rejected, reloaded!.Status);
        var history = Assert.Single(_requestRepository.GetHistoryForTest(request.Id));
        Assert.Equal(RoadsidePartnerResponse.Rejected, history.Response);
        Assert.Equal("Indisponible", history.RejectionReason);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsValidationError()
    {
        var (request, partnerUserId) = await CreatePendingResponseRequestAsync();

        var result = await _rejectHandler.Handle(new RejectRoadsideJobCommand(request.Id, partnerUserId, ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
