using SmartTaxi.Application.Advertising.Commands.SubmitCampaign;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class SubmitCampaignCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeCampaignCreativeRepository _creativeRepository = new();
    private readonly FakeAdCampaignReviewHistoryRepository _reviewHistoryRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly SubmitCampaignCommandHandler _handler;

    public SubmitCampaignCommandHandlerTests()
    {
        _handler = new SubmitCampaignCommandHandler(_campaignRepository, _creativeRepository, _reviewHistoryRepository, _notificationDispatcher);
    }

    private async Task<AdCampaign> CreateDraftCampaignAsync()
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);
        return campaign;
    }

    [Fact]
    public async Task Handle_NoMedia_ReturnsValidationError()
    {
        var campaign = await CreateDraftCampaignAsync();

        var result = await _handler.Handle(new SubmitCampaignCommand(campaign.Id, campaign.AdvertiserUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithMedia_SubmitsAndNotifiesAdvertiser()
    {
        var campaign = await CreateDraftCampaignAsync();
        var creative = CampaignCreative.Upload(campaign.Id, AdMediaType.Image, "image/png", "a.png", "key", 100, "hash", DateTime.UtcNow);
        await _creativeRepository.AddAsync(creative, CancellationToken.None);

        var result = await _handler.Handle(new SubmitCampaignCommand(campaign.Id, campaign.AdvertiserUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Submitted, reloaded!.Status);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.TemplateKey == "advertising.campaign-submitted");
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var campaign = await CreateDraftCampaignAsync();

        var result = await _handler.Handle(new SubmitCampaignCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
