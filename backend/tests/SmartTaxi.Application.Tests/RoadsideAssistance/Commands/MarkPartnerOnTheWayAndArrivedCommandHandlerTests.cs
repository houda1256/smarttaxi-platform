using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerArrived;
using SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerOnTheWay;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class MarkPartnerOnTheWayAndArrivedCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly MarkPartnerOnTheWayCommandHandler _onTheWayHandler;
    private readonly MarkPartnerArrivedCommandHandler _arrivedHandler;

    public MarkPartnerOnTheWayAndArrivedCommandHandlerTests()
    {
        _onTheWayHandler = new MarkPartnerOnTheWayCommandHandler(_requestRepository, _notificationDispatcher);
        _arrivedHandler = new MarkPartnerArrivedCommandHandler(_requestRepository, _notificationDispatcher);
    }

    private async Task<(RoadsideAssistanceRequest Request, Guid PartnerUserId)> CreateAcceptedRequestAsync()
    {
        var requesterUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur", 36.8, 10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryRespondAsync(
            request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
        return (request, partnerUserId);
    }

    [Fact]
    public async Task OnTheWay_BySelectedPartner_Succeeds()
    {
        var (request, partnerUserId) = await CreateAcceptedRequestAsync();

        var result = await _onTheWayHandler.Handle(new MarkPartnerOnTheWayCommand(request.Id, partnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PartnerOnTheWay, reloaded!.Status);
        Assert.NotNull(reloaded.PartnerOnTheWayAtUtc);
    }

    [Fact]
    public async Task OnTheWay_ByUnrelatedPartner_ReturnsConflict()
    {
        var (request, _) = await CreateAcceptedRequestAsync();

        var result = await _onTheWayHandler.Handle(new MarkPartnerOnTheWayCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Arrived_AfterOnTheWay_Succeeds()
    {
        var (request, partnerUserId) = await CreateAcceptedRequestAsync();
        await _onTheWayHandler.Handle(new MarkPartnerOnTheWayCommand(request.Id, partnerUserId), CancellationToken.None);

        var result = await _arrivedHandler.Handle(new MarkPartnerArrivedCommand(request.Id, partnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PartnerArrived, reloaded!.Status);
    }

    [Fact]
    public async Task Arrived_WithoutFirstBeingOnTheWay_ReturnsConflict()
    {
        var (request, partnerUserId) = await CreateAcceptedRequestAsync();

        var result = await _arrivedHandler.Handle(new MarkPartnerArrivedCommand(request.Id, partnerUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
