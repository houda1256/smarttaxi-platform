using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.ReselectRoadsideRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class ReselectRoadsideRequestCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly ReselectRoadsideRequestCommandHandler _handler;

    public ReselectRoadsideRequestCommandHandlerTests()
    {
        _handler = new ReselectRoadsideRequestCommandHandler(_requestRepository);
    }

    private async Task<(RoadsideAssistanceRequest Request, Guid RequesterUserId)> CreateRejectedRequestAsync()
    {
        var requesterUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur", 36.8, 10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryRespondAsync(
            request.Id, partnerUserId, RoadsidePartnerResponse.Rejected, null, "Indisponible", DateTime.UtcNow, CancellationToken.None);
        return (request, requesterUserId);
    }

    [Fact]
    public async Task Handle_RejectedRequestByItsOwnRequester_ReturnsToPartnersAvailableAndPreservesHistory()
    {
        var (request, requesterUserId) = await CreateRejectedRequestAsync();

        var result = await _handler.Handle(new ReselectRoadsideRequestCommand(request.Id, requesterUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PartnersAvailable, reloaded!.Status);
        Assert.Null(reloaded.SelectedPartnerUserId);

        var history = Assert.Single(_requestRepository.GetHistoryForTest(request.Id));
        Assert.Equal(RoadsidePartnerResponse.Rejected, history.Response);
        Assert.Equal("Indisponible", history.RejectionReason);
    }

    [Fact]
    public async Task Handle_NewCycleAfterReselect_CreatesSecondHistoryRowWithoutErasingFirst()
    {
        var (request, requesterUserId) = await CreateRejectedRequestAsync();
        await _handler.Handle(new ReselectRoadsideRequestCommand(request.Id, requesterUserId), CancellationToken.None);

        var newPartnerUserId = Guid.NewGuid();
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, newPartnerUserId, DateTime.UtcNow, CancellationToken.None);

        var history = _requestRepository.GetHistoryForTest(request.Id);
        Assert.Equal(2, history.Count);
        Assert.Equal(RoadsidePartnerResponse.Rejected, history.First().Response);
        Assert.Null(history.Last().Response);
        Assert.Equal(newPartnerUserId, history.Last().SelectedPartnerUserId);
    }

    [Fact]
    public async Task Handle_NotRejectedStatus_ReturnsConflict()
    {
        var requesterUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur", 36.8, 10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(new ReselectRoadsideRequestCommand(request.Id, requesterUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AnotherRequester_ReturnsConflict()
    {
        var (request, _) = await CreateRejectedRequestAsync();

        var result = await _handler.Handle(new ReselectRoadsideRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
