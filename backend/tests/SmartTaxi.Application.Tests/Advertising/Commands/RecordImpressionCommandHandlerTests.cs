using SmartTaxi.Application.Advertising.Commands.RecordImpression;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class RecordImpressionCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdvertisingImpressionRepository _impressionRepository;
    private readonly FakeAdDeliveryTokenService _tokenService = new();
    private readonly RecordImpressionCommandHandler _handler;

    public RecordImpressionCommandHandlerTests()
    {
        _impressionRepository = new FakeAdvertisingImpressionRepository(_campaignRepository);
        _handler = new RecordImpressionCommandHandler(_campaignRepository, _impressionRepository, _tokenService);
    }

    private async Task<AdCampaign> CreateCampaignAsync(AdCampaignStatus status, decimal budgetLimit = 1000m, AdPricingModel pricingModel = AdPricingModel.Cpm, decimal priceRate = 10m)
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
    public async Task Handle_NonActiveCampaign_ReturnsConflict()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Draft);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_PlacementMismatch_ReturnsValidationError()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, Guid.NewGuid(), userId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ValidToken_RecordsImpressionAndConsumesBudget()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 1000m, pricingModel: AdPricingModel.Cpm, priceRate: 10m);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(0.01m, reloaded!.ConsumedBudget); // 10 TND per 1000 impressions => 0.01 per impression
    }

    [Fact]
    public async Task Handle_BudgetExhausted_ReturnsConflict()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 0.005m, pricingModel: AdPricingModel.Cpm, priceRate: 10m);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_SameDeliveryTokenReplayed_DoesNotDoubleConsumeBudget()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 1000m, pricingModel: AdPricingModel.Cpm, priceRate: 10m);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var first = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow), CancellationToken.None);
        var second = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(0.01m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Handle_ForgedToken_IsRejected()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, "not-a-real-token", DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(0m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Handle_ExpiredToken_IsRejected()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);
        _tokenService.ExpireToken(token);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_TokenIssuedForDifferentCampaign_IsRejected()
    {
        var campaignA = await CreateCampaignAsync(AdCampaignStatus.Active);
        var campaignB = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var tokenForA = _tokenService.IssueToken(campaignA.Id, campaignA.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaignB.Id, campaignB.PlacementId, userId, tokenForA, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_TokenIssuedForDifferentPlacement_IsRejected()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var tokenForOtherPlacement = _tokenService.IssueToken(campaign.Id, Guid.NewGuid(), userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, tokenForOtherPlacement, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_TokenIssuedForDifferentUser_IsRejected()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var issuedForUserId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, issuedForUserId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, requestingUserId, token, DateTime.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WildlyInvalidOccurredAtUtc_ReturnsValidationError()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active);
        var userId = Guid.NewGuid();
        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, userId, DateTime.UtcNow);

        var result = await _handler.Handle(
            new RecordImpressionCommand(campaign.Id, campaign.PlacementId, userId, token, DateTime.UtcNow.AddDays(-5)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
