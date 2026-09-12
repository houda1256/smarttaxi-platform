using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetRoadsideAssistanceRequestById;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Queries;

public class GetRoadsideAssistanceRequestByIdQueryHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly GetRoadsideAssistanceRequestByIdQueryHandler _handler;

    public GetRoadsideAssistanceRequestByIdQueryHandlerTests()
    {
        _handler = new GetRoadsideAssistanceRequestByIdQueryHandler(_requestRepository);
    }

    private async Task<(RoadsideAssistanceRequest Request, Guid RequesterUserId, Guid PartnerUserId)> CreateSelectedRequestAsync()
    {
        var requesterUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        return (request, requesterUserId, partnerUserId);
    }

    [Fact]
    public async Task Handle_AsRequester_Succeeds()
    {
        var (request, requesterUserId, _) = await CreateSelectedRequestAsync();

        var result = await _handler.Handle(new GetRoadsideAssistanceRequestByIdQuery(request.Id, requesterUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AsSelectedPartner_Succeeds()
    {
        var (request, _, partnerUserId) = await CreateSelectedRequestAsync();

        var result = await _handler.Handle(new GetRoadsideAssistanceRequestByIdQuery(request.Id, partnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AsUnrelatedUser_ReturnsForbidden()
    {
        var (request, _, _) = await CreateSelectedRequestAsync();

        var result = await _handler.Handle(new GetRoadsideAssistanceRequestByIdQuery(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_UnknownRequest_ReturnsNotFound()
    {
        var result = await _handler.Handle(new GetRoadsideAssistanceRequestByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
