using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Queries;

public class GetCampaignPerformanceQueryHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdvertisingImpressionRepository _impressionRepository;
    private readonly FakeAdvertisingClickRepository _clickRepository;
    private readonly GetCampaignPerformanceQueryHandler _handler;

    public GetCampaignPerformanceQueryHandlerTests()
    {
        _impressionRepository = new FakeAdvertisingImpressionRepository(_campaignRepository);
        _clickRepository = new FakeAdvertisingClickRepository(_campaignRepository);
        _handler = new GetCampaignPerformanceQueryHandler(_campaignRepository, _impressionRepository, _clickRepository);
    }

    private async Task<AdCampaign> CreateActiveCampaignAsync()
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Cpc, 1m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);
        await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], AdCampaignStatus.Active, false, null, null, DateTime.UtcNow, CancellationToken.None);
        return campaign;
    }

    [Fact]
    public async Task Handle_NoImpressionsYet_CtrIsZeroNotDivideByZero()
    {
        var campaign = await CreateActiveCampaignAsync();

        var result = await _handler.Handle(new GetCampaignPerformanceQuery(campaign.Id, campaign.AdvertiserUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Impressions);
        Assert.Equal(0m, result.Value.Ctr);
    }

    [Fact]
    public async Task Handle_WithImpressionsAndClicks_ComputesCtr()
    {
        var campaign = await CreateActiveCampaignAsync();
        await _impressionRepository.TryRecordAsync(campaign.Id, campaign.PlacementId, "imp-1", 0m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);
        await _impressionRepository.TryRecordAsync(campaign.Id, campaign.PlacementId, "imp-2", 0m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);
        await _clickRepository.TryRecordAsync(campaign.Id, campaign.PlacementId, null, "click-1", 1m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new GetCampaignPerformanceQuery(campaign.Id, campaign.AdvertiserUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Impressions);
        Assert.Equal(1, result.Value.Clicks);
        Assert.Equal(0.5m, result.Value.Ctr);
    }

    [Fact]
    public async Task Handle_AnotherAdvertiser_ReturnsForbidden()
    {
        var campaign = await CreateActiveCampaignAsync();

        var result = await _handler.Handle(new GetCampaignPerformanceQuery(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
