using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Advertising.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves, against real PostgreSQL, that a duplicate Impression/Click IdempotencyKey never double-consumes budget — the Module 8 mandatory anti-fraud/anti-duplication guarantee.</summary>
[Collection("SharedPostgres")]
public class AdvertisingFactConcurrencyTests
{
    private readonly SharedPostgresFixture _fixture;

    public AdvertisingFactConcurrencyTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<AdCampaign> CreateActiveCampaignAsync(AdPricingModel pricingModel, decimal priceRate, decimal budgetLimit = 1000m)
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10),
            pricingModel, priceRate, budgetLimit, null, null, null, null, null, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new AdCampaignRepository(context).AddAsync(campaign, CancellationToken.None);

        await using var transitionContext = _fixture.CreateContext();
        await new AdCampaignRepository(transitionContext).TryTransitionAsync(
            campaign.Id, [AdCampaignStatus.Draft], AdCampaignStatus.Active, false, null, null, DateTime.UtcNow, CancellationToken.None);

        return campaign;
    }

    [Fact]
    public async Task Impression_TwoConcurrentAttemptsWithSameIdempotencyKey_OnlyOneConsumesBudget()
    {
        var campaign = await CreateActiveCampaignAsync(AdPricingModel.Cpm, 10m);
        const string idempotencyKey = "same-impression-key";

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new AdvertisingImpressionRepository(context1).TryRecordAsync(campaign.Id, campaign.PlacementId, idempotencyKey, 0.01m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None),
            new AdvertisingImpressionRepository(context2).TryRecordAsync(campaign.Id, campaign.PlacementId, idempotencyKey, 0.01m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r.Outcome == AdvertisingFactOutcome.Created);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(0.01m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Impression_RetryWithSameIdempotencyKeyAfterSuccess_ReplaysWithoutDoubleConsumption()
    {
        var campaign = await CreateActiveCampaignAsync(AdPricingModel.Cpm, 10m);

        await using var context = _fixture.CreateContext();
        var repository = new AdvertisingImpressionRepository(context);

        var first = await repository.TryRecordAsync(campaign.Id, campaign.PlacementId, "key-1", 0.01m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);
        var second = await repository.TryRecordAsync(campaign.Id, campaign.PlacementId, "key-1", 0.01m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(AdvertisingFactOutcome.Created, first.Outcome);
        Assert.Equal(AdvertisingFactOutcome.Replayed, second.Outcome);
        Assert.Equal(first.FactId, second.FactId);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(0.01m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Click_TwoConcurrentAttemptsWithSameIdempotencyKey_OnlyOneConsumesBudget()
    {
        var campaign = await CreateActiveCampaignAsync(AdPricingModel.Cpc, 2m);
        const string idempotencyKey = "same-click-key";

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new AdvertisingClickRepository(context1).TryRecordAsync(campaign.Id, campaign.PlacementId, null, idempotencyKey, 2m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None),
            new AdvertisingClickRepository(context2).TryRecordAsync(campaign.Id, campaign.PlacementId, null, idempotencyKey, 2m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r.Outcome == AdvertisingFactOutcome.Created);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(2m, reloaded!.ConsumedBudget);
    }

    [Fact]
    public async Task Impression_BudgetExhaustedByConcurrentClicks_CannotExceedLimit()
    {
        var campaign = await CreateActiveCampaignAsync(AdPricingModel.Cpc, 60m, budgetLimit: 100m);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new AdvertisingClickRepository(context1).TryRecordAsync(campaign.Id, campaign.PlacementId, null, "click-a", 60m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None),
            new AdvertisingClickRepository(context2).TryRecordAsync(campaign.Id, campaign.PlacementId, null, "click-b", 60m, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r.Outcome == AdvertisingFactOutcome.Created);
        Assert.Single(results, r => r.Outcome == AdvertisingFactOutcome.BudgetExceeded);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(60m, reloaded!.ConsumedBudget);
        Assert.True(reloaded.ConsumedBudget <= reloaded.BudgetLimit);
    }
}
