using SmartTaxi.Application.RoadsideAssistance.Queries.GetAllRoadsideAssistanceRequests;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideAssistanceRequests;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideJobs;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Queries;

public class GetMyRoadsideAssistanceRequestsAndJobsQueryHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();

    private async Task<RoadsideAssistanceRequest> CreateRequestAsync(Guid requesterUserId)
    {
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task GetMyRequests_ReturnsOnlyOwnRequests()
    {
        var requesterUserId = Guid.NewGuid();
        await CreateRequestAsync(requesterUserId);
        await CreateRequestAsync(Guid.NewGuid());

        var handler = new GetMyRoadsideAssistanceRequestsQueryHandler(_requestRepository);
        var result = await handler.Handle(new GetMyRoadsideAssistanceRequestsQuery(requesterUserId, 1, 10), CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetMyJobs_ReturnsOnlyRequestsWherePartnerWasSelected()
    {
        var requesterUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var selected = await CreateRequestAsync(requesterUserId);
        await _requestRepository.TrySelectPartnerAsync(selected.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await CreateRequestAsync(requesterUserId); // never selects this partner

        var handler = new GetMyRoadsideJobsQueryHandler(_requestRepository);
        var result = await handler.Handle(new GetMyRoadsideJobsQuery(partnerUserId, 1, 10), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(selected.Id, result.Items.Single().Id);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryRequest()
    {
        await CreateRequestAsync(Guid.NewGuid());
        await CreateRequestAsync(Guid.NewGuid());

        var handler = new GetAllRoadsideAssistanceRequestsQueryHandler(_requestRepository);
        var result = await handler.Handle(new GetAllRoadsideAssistanceRequestsQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
    }
}
