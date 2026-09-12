using SmartTaxi.Application.Advertising.Commands.RequestAdDelivery;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class RequestAdDeliveryCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdDeliveryTokenService _tokenService = new();
    private readonly RequestAdDeliveryCommandHandler _handler;

    public RequestAdDeliveryCommandHandlerTests()
    {
        _handler = new RequestAdDeliveryCommandHandler(_campaignRepository, _tokenService);
    }

    private async Task<AdCampaign> CreateCampaignAsync(AdCampaignStatus status, DateTime? endAtUtc = null)
    {
        var placementId = Guid.NewGuid();
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", placementId, DateTime.UtcNow.AddDays(-1), endAtUtc ?? DateTime.UtcNow.AddDays(10),
            AdPricingModel.Cpm, 10m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);

        if (status != AdCampaignStatus.Draft)
        {
            await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], status, false, null, null, DateTime.UtcNow, CancellationToken.None);
        }

        return campaign;
    }

    [Fact]
    public async Task Handle_ActiveCampaign_IssuesToken()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(new RequestAdDeliveryCommand(campaign.Id, campaign.PlacementId, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value));
    }

    [Fact]
    public async Task Handle_NonActiveCampaign_ReturnsConflict()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Draft);
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(new RequestAdDeliveryCommand(campaign.Id, campaign.PlacementId, userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_CampaignPastEndAtUtc_ReturnsConflictEvenIfStillActive()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, endAtUtc: DateTime.UtcNow.AddSeconds(-1));
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(new RequestAdDeliveryCommand(campaign.Id, campaign.PlacementId, userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_PlacementMismatch_ReturnsValidationError()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(new RequestAdDeliveryCommand(campaign.Id, Guid.NewGuid(), userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        var result = await _handler.Handle(new RequestAdDeliveryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
