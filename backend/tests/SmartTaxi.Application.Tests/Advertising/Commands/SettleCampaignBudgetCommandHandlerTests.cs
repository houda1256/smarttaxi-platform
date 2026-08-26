using SmartTaxi.Application.Advertising.Commands.SettleCampaignBudget;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class SettleCampaignBudgetCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdvertisingBillingService _billingService = new();
    private readonly SettleCampaignBudgetCommandHandler _handler;

    public SettleCampaignBudgetCommandHandlerTests()
    {
        _handler = new SettleCampaignBudgetCommandHandler(_campaignRepository, _billingService);
    }

    private async Task<AdCampaign> CreateCampaignAsync(AdCampaignStatus status, decimal consumedBudget)
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1),
            AdPricingModel.Cpm, 10m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);
        await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], AdCampaignStatus.Active, false, null, null, DateTime.UtcNow, CancellationToken.None);

        if (consumedBudget > 0)
        {
            await _campaignRepository.TryConsumeBudgetAsync(campaign.Id, consumedBudget, DateTime.UtcNow, CancellationToken.None);
        }

        await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Active], status, false, null, null, DateTime.UtcNow, CancellationToken.None);
        return campaign;
    }

    [Fact]
    public async Task Handle_EligibleCampaignWithConsumedBudget_SettlesSuccessfully()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Completed, 50m);

        var result = await _handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_billingService.SettledCalls);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_AlreadySettled_ReturnsConflictWithoutPostingAgain()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Completed, 50m);
        var first = await _handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
        Assert.Single(_billingService.SettledCalls);
    }

    [Fact]
    public async Task Handle_NotEligibleStatus_ReturnsConflict()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, 0m);

        var result = await _handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(_billingService.SettledCalls);
    }

    [Fact]
    public async Task Handle_NothingConsumed_ReturnsValidationError()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Completed, 0m);

        var result = await _handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(_billingService.SettledCalls);
    }

    /// <summary>Fix 2's core scenario: the ledger entry was already posted (as if PostBatchAsync had committed in
    /// a prior, crashed attempt) but SettledAtUtc is still null. The retry must converge — mark settled — without
    /// ever posting a second ledger entry.</summary>
    [Fact]
    public async Task Handle_OrphanedLedgerEntryFromPriorCrash_ConvergesWithoutDoublePosting()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Completed, 50m);
        _billingService.CampaignIdsWithOrphanedLedgerEntry.Add(campaign.Id);

        var result = await _handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_billingService.SettledCalls);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        var result = await _handler.Handle(new SettleCampaignBudgetCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
