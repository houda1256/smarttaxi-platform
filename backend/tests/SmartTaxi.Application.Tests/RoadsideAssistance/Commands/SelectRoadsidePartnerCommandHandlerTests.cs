using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.SelectRoadsidePartner;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class SelectRoadsidePartnerCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeRoadsidePartnerProfileRepository _partnerProfileRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly SelectRoadsidePartnerCommandHandler _handler;

    public SelectRoadsidePartnerCommandHandlerTests()
    {
        _handler = new SelectRoadsidePartnerCommandHandler(_requestRepository, _partnerProfileRepository, _notificationDispatcher);
    }

    private async Task<RoadsideAssistanceRequest> CreateRequestAsync(Guid requesterUserId)
    {
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur", 36.8, 10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    private async Task<Guid> CreateActivePartnerAsync()
    {
        var userId = Guid.NewGuid();
        var profile = RoadsidePartnerProfile.Register(userId, "Assistance Rapide", null, "12 rue X", "Tunis", null, null, null, null, DateTime.UtcNow);
        await _partnerProfileRepository.TryAddAsync(profile, CancellationToken.None);
        return userId;
    }

    [Fact]
    public async Task Handle_EligibleRequestAndActivePartner_Succeeds()
    {
        var requesterUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(requesterUserId);
        var partnerUserId = await CreateActivePartnerAsync();

        var result = await _handler.Handle(new SelectRoadsidePartnerCommand(request.Id, requesterUserId, partnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PendingPartnerResponse, reloaded!.Status);
        Assert.Equal(partnerUserId, reloaded.SelectedPartnerUserId);
        Assert.Single(_requestRepository.GetHistoryForTest(request.Id));
        Assert.Single(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_UnknownPartner_ReturnsNotFound()
    {
        var requesterUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(requesterUserId);

        var result = await _handler.Handle(new SelectRoadsidePartnerCommand(request.Id, requesterUserId, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AnotherRequester_ReturnsConflict()
    {
        var requesterUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(requesterUserId);
        var partnerUserId = await CreateActivePartnerAsync();

        var result = await _handler.Handle(new SelectRoadsidePartnerCommand(request.Id, Guid.NewGuid(), partnerUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_TwoConcurrentSelections_OnlyOneSucceeds()
    {
        var requesterUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(requesterUserId);
        var partnerUserId = await CreateActivePartnerAsync();

        var first = await _handler.Handle(new SelectRoadsidePartnerCommand(request.Id, requesterUserId, partnerUserId), CancellationToken.None);
        var second = await _handler.Handle(new SelectRoadsidePartnerCommand(request.Id, requesterUserId, partnerUserId), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
    }
}
