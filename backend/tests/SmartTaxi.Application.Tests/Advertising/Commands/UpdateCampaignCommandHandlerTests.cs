using SmartTaxi.Application.Advertising.Commands.UpdateCampaign;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class UpdateCampaignCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdvertisingPlacementRepository _placementRepository = new();
    private readonly FakeAdCampaignReviewHistoryRepository _reviewHistoryRepository = new();
    private readonly UpdateCampaignCommandHandler _handler;
    private readonly AdvertisingPlacement _placement;

    public UpdateCampaignCommandHandlerTests()
    {
        _handler = new UpdateCampaignCommandHandler(_campaignRepository, _placementRepository, _reviewHistoryRepository);
        _placement = AdvertisingPlacement.Create("SCREEN", "Screen", "desc", [AdMediaType.Image], DateTime.UtcNow);
        _placementRepository.Seed(_placement);
    }

    private async Task<AdCampaign> CreateCampaignAsync(AdCampaignStatus status)
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", _placement.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Flat, 100m, 1000m, null, "Tunis", null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);

        if (status != AdCampaignStatus.Draft)
        {
            await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], status, true, Guid.NewGuid(), null, DateTime.UtcNow, CancellationToken.None);
        }

        return campaign;
    }

    private UpdateCampaignCommand BuildCommand(AdCampaign campaign, Guid requestingUserId, decimal budgetLimit) =>
        new(campaign.Id, requestingUserId, "New Name", "New Desc", campaign.Objective, campaign.PlacementId, campaign.StartAtUtc,
            campaign.EndAtUtc, campaign.PricingModel, campaign.PriceRate, budgetLimit, campaign.DailyBudgetLimit, campaign.TargetCity,
            campaign.TargetVehicleCategory, campaign.TargetDaysOfWeek, campaign.TargetStartHour, campaign.TargetEndHour);

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Draft);

        var result = await _handler.Handle(BuildCommand(campaign, Guid.NewGuid(), campaign.BudgetLimit), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_DraftCosmeticEdit_Succeeds()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Draft);

        var result = await _handler.Handle(BuildCommand(campaign, campaign.AdvertiserUserId, campaign.BudgetLimit), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal("New Name", reloaded!.Name);
        Assert.Equal(AdCampaignStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task Handle_MaterialChangeOnApprovedCampaign_ForcesResubmissionAndRecordsHistory()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Approved);

        var result = await _handler.Handle(BuildCommand(campaign, campaign.AdvertiserUserId, campaign.BudgetLimit + 500m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Submitted, reloaded!.Status);
        Assert.Null(reloaded.ReviewedByUserId);
        Assert.Contains(_reviewHistoryRepository.All, e => e.Action == AdCampaignReviewAction.ResubmittedAfterMaterialChange);
    }

    [Fact]
    public async Task Handle_CosmeticOnlyChangeOnApprovedCampaign_DoesNotForceResubmission()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Approved);

        var result = await _handler.Handle(BuildCommand(campaign, campaign.AdvertiserUserId, campaign.BudgetLimit), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Approved, reloaded!.Status);
        Assert.Equal("New Name", reloaded.Name);
    }

    [Fact]
    public async Task Handle_SubmittedCampaign_CannotBeEdited()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Submitted);

        var result = await _handler.Handle(BuildCommand(campaign, campaign.AdvertiserUserId, campaign.BudgetLimit), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
