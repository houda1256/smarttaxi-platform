using SmartTaxi.Application.Advertising.Commands.RecordClick;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class RecordClickCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdvertisingImpressionRepository _impressionRepository;
    private readonly FakeAdvertisingClickRepository _clickRepository;
    private readonly FakeAdDeliveryTokenService _tokenService = new();
    private readonly RecordClickCommandHandler _handler;

    public RecordClickCommandHandlerTests()
    {
        _impressionRepository = new FakeAdvertisingImpressionRepository(_campaignRepository);
        _clickRepository = new FakeAdvertisingClickRepository(_campaignRepository);
        _handler = new RecordClickCommandHandler(_campaignRepository, _impressionRepository, _clickRepository, _tokenService);
    }

    private async Task<AdCampaign> CreateCampaignAsync(AdCampaignStatus status, decimal budgetLimit = 1000m, AdPricingModel pricingModel = AdPricingModel.Cpc, decimal priceRate = 1m)
    {
        var placementId = Guid.NewGuid();
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", placementId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10),
            pricingModel, priceRate, budgetLimit, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);

        if (status != AdCampaignStatus.Draft)
        {
            await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], status, false, null, null, DateTime.UtcNow, CancellationToken.None);
        }

        return campaign;
    }

    [Fact]
    public async Task Handle_ValidToken_RecordsClickAndConsumesBudget()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 100m, pricingModel: AdPricingModel.Cpc, priceRate: 1m);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordClickCommand(campaign.Id, campaign.PlacementId, userId, null, token, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(1m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Handle_WithoutLegitimateDeliveryContext_IsRejected()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(
            new RecordClickCommand(campaign.Id, campaign.PlacementId, userId, null, "fabricated-token", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(0m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Handle_SameDeliveryTokenReplayed_DoesNotDoubleConsumeBudget()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 100m, pricingModel: AdPricingModel.Cpc, priceRate: 1m);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var first = await _handler.Handle(
            new RecordClickCommand(campaign.Id, campaign.PlacementId, userId, null, token, DateTime.UtcNow), CancellationToken.None);
        var second = await _handler.Handle(
            new RecordClickCommand(campaign.Id, campaign.PlacementId, userId, null, token, DateTime.UtcNow), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(1m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Handle_TokenIssuedForDifferentCampaign_IsRejected()
    {
        var campaignA = await CreateCampaignAsync(AdCampaignStatus.Active);
        var campaignB = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var tokenForA = _tokenService.IssueToken(campaignA.Id, campaignA.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordClickCommand(campaignB.Id, campaignB.PlacementId, userId, null, tokenForA, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ImpressionFromDifferentCampaign_ReturnsValidationError()
    {
        var campaignA = await CreateCampaignAsync(AdCampaignStatus.Active);
        var campaignB = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var impressionResult = await _impressionRepository.TryRecordAsync(
            campaignA.Id, campaignA.PlacementId, "impression-key", 0m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);

        var clickToken = _tokenService.IssueToken(campaignB.Id, campaignB.PlacementId, userId, DateTime.UtcNow);
        var result = await _handler.Handle(
            new RecordClickCommand(campaignB.Id, campaignB.PlacementId, userId, impressionResult.FactId, clickToken, DateTime.UtcNow),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
