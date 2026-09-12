using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Advertising.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against a real PostgreSQL database, the Module 8 mandatory
/// concurrency guarantees: approve-vs-reject and two-concurrent-activation
/// races each resolve to exactly one winner (atomic conditional UPDATE on
/// Status), and concurrent budget consumption can never together exceed a
/// campaign's BudgetLimit.
/// </summary>
[Collection("SharedPostgres")]
public class AdCampaignConcurrencyTests
{
    private readonly SharedPostgresFixture _fixture;

    public AdCampaignConcurrencyTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<AdCampaign> CreateCampaignAsync(AdCampaignStatus status, decimal budgetLimit = 1000m)
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Cpm, 10m, budgetLimit, null, null, null, null, null, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new AdCampaignRepository(context).AddAsync(campaign, CancellationToken.None);

        if (status != AdCampaignStatus.Draft)
        {
            await using var transitionContext = _fixture.CreateContext();
            await new AdCampaignRepository(transitionContext).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Draft], status, touchReviewMetadata: false, null, null, DateTime.UtcNow, CancellationToken.None);
        }

        return campaign;
    }

    [Fact]
    public async Task TryTransitionAsync_ApproveVsRejectRace_ExactlyOneWins()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Submitted);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new AdCampaignRepository(context1).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview], AdCampaignStatus.Approved, true, Guid.NewGuid(), null,
                DateTime.UtcNow, CancellationToken.None),
            new AdCampaignRepository(context2).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview], AdCampaignStatus.Rejected, true, Guid.NewGuid(),
                "Non conforme", DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.True(reloaded!.Status is AdCampaignStatus.Approved or AdCampaignStatus.Rejected);
    }

    [Fact]
    public async Task TryTransitionAsync_TwoConcurrentActivationAttempts_ExactlyOneSucceeds()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Scheduled);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new AdCampaignRepository(context1).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Scheduled], AdCampaignStatus.Active, false, null, null, DateTime.UtcNow, CancellationToken.None),
            new AdCampaignRepository(context2).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Scheduled], AdCampaignStatus.Active, false, null, null, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Active, reloaded!.Status);
    }

    [Fact]
    public async Task TryConsumeBudgetAsync_ConcurrentConsumptionExceedingRemainingBudget_NeverExceedsLimit()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 100m);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new AdCampaignRepository(context1).TryConsumeBudgetAsync(campaign.Id, 60m, DateTime.UtcNow, CancellationToken.None),
            new AdCampaignRepository(context2).TryConsumeBudgetAsync(campaign.Id, 60m, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(60m, reloaded!.ConsumedBudget);
        Assert.True(reloaded.ConsumedBudget <= reloaded.BudgetLimit);
    }

    [Fact]
    public async Task TryConsumeBudgetAsync_ConcurrentConsumptionWithinBudget_AllSucceedAndSumCorrectly()
    {
        var campaign = await CreateCampaignAsync(AdCampaignStatus.Active, budgetLimit: 1000m);

        // Each concurrent operation needs its own DbContext instance — a single DbContext is not thread-safe.
        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var context = _fixture.CreateContext();
            return await new AdCampaignRepository(context).TryConsumeBudgetAsync(campaign.Id, 10m, DateTime.UtcNow, CancellationToken.None);
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.True);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(200m, reloaded!.ConsumedBudget);
    }
}
