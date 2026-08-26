using SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Queries;

public class GetCampaignDetailsQueryHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeCampaignCreativeRepository _creativeRepository = new();
    private readonly FakeAdCampaignReviewHistoryRepository _reviewHistoryRepository = new();
    private readonly GetCampaignDetailsQueryHandler _handler;

    public GetCampaignDetailsQueryHandlerTests()
    {
        _handler = new GetCampaignDetailsQueryHandler(_campaignRepository, _creativeRepository, _reviewHistoryRepository);
    }

    [Fact]
    public async Task Handle_AnotherAdvertisersCampaign_ReturnsForbidden()
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);

        var result = await _handler.Handle(new GetCampaignDetailsQuery(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsDetails()
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);

        var result = await _handler.Handle(new GetCampaignDetailsQuery(campaign.Id, campaign.AdvertiserUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(campaign.Id, result.Value!.Campaign.Id);
    }
}
