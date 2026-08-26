using SmartTaxi.Application.RoadsideAssistance.Commands.ExpireStaleRoadsideRequests;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class ExpireStaleRoadsideRequestsCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeRoadsideExpiryPolicy _expiryPolicy = new() { StaleAfter = TimeSpan.FromHours(1) };
    private readonly ExpireStaleRoadsideRequestsCommandHandler _handler;

    public ExpireStaleRoadsideRequestsCommandHandlerTests()
    {
        _handler = new ExpireStaleRoadsideRequestsCommandHandler(_requestRepository, _expiryPolicy);
    }

    private async Task<RoadsideAssistanceRequest> CreateRequestAsync(DateTime requestedAtUtc)
    {
        var request = RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, null, requestedAtUtc);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_StaleRequestBeyondThreshold_ExpiresIt()
    {
        var request = await CreateRequestAsync(DateTime.UtcNow.AddHours(-3));

        var result = await _handler.Handle(new ExpireStaleRoadsideRequestsCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Expired, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_RecentRequest_IsNotExpired()
    {
        var request = await CreateRequestAsync(DateTime.UtcNow.AddMinutes(-10));

        var result = await _handler.Handle(new ExpireStaleRoadsideRequestsCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PartnersAvailable, reloaded!.Status);
    }
}
