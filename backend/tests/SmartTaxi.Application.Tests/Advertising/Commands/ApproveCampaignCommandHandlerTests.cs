using SmartTaxi.Application.Advertising.Commands.ApproveCampaign;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class ApproveCampaignCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeCampaignCreativeRepository _creativeRepository = new();
    private readonly FakeAdCampaignReviewHistoryRepository _reviewHistoryRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly ApproveCampaignCommandHandler _handler;

    public ApproveCampaignCommandHandlerTests()
    {
        _handler = new ApproveCampaignCommandHandler(_campaignRepository, _creativeRepository, _reviewHistoryRepository, _notificationDispatcher);
    }

    private async Task<AdCampaign> CreateSubmittedCampaignAsync()
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);
        await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], AdCampaignStatus.Submitted, true, null, null, DateTime.UtcNow, CancellationToken.None);
        return campaign;
    }

    [Fact]
    public async Task Handle_SelfReview_ReturnsForbidden()
    {
        var campaign = await CreateSubmittedCampaignAsync();

        var result = await _handler.Handle(new ApproveCampaignCommand(campaign.Id, campaign.AdvertiserUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Submitted, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_DifferentReviewer_ApprovesCampaignAndPendingCreatives()
    {
        var campaign = await CreateSubmittedCampaignAsync();
        var creative = CampaignCreative.Upload(campaign.Id, AdMediaType.Image, "image/png", "a.png", "key", 100, "hash", DateTime.UtcNow);
        await _creativeRepository.AddAsync(creative, CancellationToken.None);
        var reviewerId = Guid.NewGuid();

        var result = await _handler.Handle(new ApproveCampaignCommand(campaign.Id, reviewerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Approved, reloaded!.Status);
        Assert.Equal(reviewerId, reloaded.ReviewedByUserId);
        var reloadedCreative = await _creativeRepository.GetByIdAsync(creative.Id, CancellationToken.None);
        Assert.Equal(AdMediaStatus.Approved, reloadedCreative!.Status);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.TemplateKey == "advertising.campaign-approved");
    }
}
