using SmartTaxi.Application.Advertising.Commands.RejectCampaign;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class RejectCampaignCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdCampaignReviewHistoryRepository _reviewHistoryRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly RejectCampaignCommandHandler _handler;

    public RejectCampaignCommandHandlerTests()
    {
        _handler = new RejectCampaignCommandHandler(_campaignRepository, _reviewHistoryRepository, _notificationDispatcher);
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
    public async Task Handle_MissingReason_ReturnsValidationError()
    {
        var campaign = await CreateSubmittedCampaignAsync();

        var result = await _handler.Handle(new RejectCampaignCommand(campaign.Id, Guid.NewGuid(), ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_SelfReview_ReturnsForbidden()
    {
        var campaign = await CreateSubmittedCampaignAsync();

        var result = await _handler.Handle(new RejectCampaignCommand(campaign.Id, campaign.AdvertiserUserId, "Contenu non conforme"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ValidReject_TransitionsToRejectedWithReason()
    {
        var campaign = await CreateSubmittedCampaignAsync();

        var result = await _handler.Handle(new RejectCampaignCommand(campaign.Id, Guid.NewGuid(), "Contenu non conforme"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Rejected, reloaded!.Status);
        Assert.Equal("Contenu non conforme", reloaded.ReviewReason);
    }
}
