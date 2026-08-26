using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising;
using SmartTaxi.Application.Advertising.Commands.SettleCampaignBudget;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Advertising.Repositories;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against real PostgreSQL, that Module 8 audit fix #2's settlement
/// recovery/convergence path actually works end-to-end: the ledger posting
/// (PostBatchAsync, which commits its own transaction internally) and the
/// campaign's SettledAtUtc flag are genuinely two separate writes, and a
/// crash between them (simulated here by posting directly through the real
/// repositories and deliberately never calling TryMarkSettledAsync) is
/// recoverable on the next handler invocation without ever posting a second
/// ledger entry.
/// </summary>
[Collection("SharedPostgres")]
public class SettleCampaignBudgetIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public SettleCampaignBudgetIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<AdCampaign> CreateCompletedCampaignWithConsumedBudgetAsync(decimal consumedBudget)
    {
        // EndAtUtc is deliberately still in the future at creation time: TryConsumeBudgetAsync's
        // defense-in-depth guard (Module 8 audit LOW finding #1) requires EndAtUtc > utcNow at the moment
        // budget is consumed, same as during a real campaign's active lifetime — only the later explicit
        // TryTransitionAsync call below moves it to Completed for this test's settlement scenario.
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", Guid.NewGuid(), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(1),
            AdPricingModel.Cpm, 10m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new AdCampaignRepository(context).AddAsync(campaign, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            await new AdCampaignRepository(context).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Draft], AdCampaignStatus.Active, false, null, null, DateTime.UtcNow, CancellationToken.None);
        }

        if (consumedBudget > 0)
        {
            await using var context = _fixture.CreateContext();
            await new AdCampaignRepository(context).TryConsumeBudgetAsync(campaign.Id, consumedBudget, DateTime.UtcNow, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            await new AdCampaignRepository(context).TryTransitionAsync(
                campaign.Id, [AdCampaignStatus.Active], AdCampaignStatus.Completed, false, null, null, DateTime.UtcNow, CancellationToken.None);
        }

        return campaign;
    }

    [Fact]
    public async Task Handle_NormalSettlement_PostsOneLedgerEntryAndMarksSettled()
    {
        var campaign = await CreateCompletedCampaignWithConsumedBudgetAsync(25m);

        await using var context = _fixture.CreateContext();
        var campaignRepository = new AdCampaignRepository(context);
        var billingService = new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
        var handler = new SettleCampaignBudgetCommandHandler(campaignRepository, billingService);

        var result = await handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);

        var entries = await readContext.FinancialLedgerEntries
            .Where(e => e.SourceType == "AdvertisingCampaign" && e.SourceId == campaign.Id).ToListAsync();
        Assert.Single(entries);
        Assert.Equal(LedgerEntryType.AdvertisingRevenue, entries[0].EntryType);
    }

    [Fact]
    public async Task Handle_DuplicateSettlementAttempt_ReturnsConflictWithoutPostingAgain()
    {
        var campaign = await CreateCompletedCampaignWithConsumedBudgetAsync(25m);

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleCampaignBudgetCommandHandler(
                new AdCampaignRepository(context), new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var first = await handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(first.IsSuccess);
        }

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleCampaignBudgetCommandHandler(
                new AdCampaignRepository(context), new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var second = await handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.False(second.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "AdvertisingCampaign" && e.SourceId == campaign.Id);
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task Handle_ConcurrentSettlementRequests_PostsExactlyOneLedgerEntry()
    {
        var campaign = await CreateCompletedCampaignWithConsumedBudgetAsync(40m);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var handler1 = new SettleCampaignBudgetCommandHandler(
            new AdCampaignRepository(context1), new AdvertisingBillingService(new FinancialAccountRepository(context1), new FinancialLedgerRepository(context1)));
        var handler2 = new SettleCampaignBudgetCommandHandler(
            new AdCampaignRepository(context2), new AdvertisingBillingService(new FinancialAccountRepository(context2), new FinancialLedgerRepository(context2)));

        var results = await Task.WhenAll(
            handler1.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None),
            handler2.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains(results, r => r.IsSuccess);

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "AdvertisingCampaign" && e.SourceId == campaign.Id);
        Assert.Equal(1, entryCount);

        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    /// <summary>The exact crash scenario from the audit: the ledger entry is posted directly through the real
    /// billing service (as PostBatchAsync would have already committed before a crash), but SettledAtUtc is
    /// deliberately never set — simulating the process dying between the two writes. The handler's next
    /// invocation must converge to Settled without posting a second entry.</summary>
    [Fact]
    public async Task Handle_SimulatedCrashBetweenLedgerPostAndSettledFlag_ConvergesOnRetryWithoutDoublePosting()
    {
        var campaign = await CreateCompletedCampaignWithConsumedBudgetAsync(15m);

        await using (var context = _fixture.CreateContext())
        {
            var billingService = new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
            var posted = await billingService.TrySettleCampaignAsync(campaign.Id, campaign.AdvertiserUserId, 15m, "TND", DateTime.UtcNow, CancellationToken.None);
            Assert.True(posted);
        }

        await using (var preCheckContext = _fixture.CreateContext())
        {
            var reloaded = await new AdCampaignRepository(preCheckContext).GetByIdAsync(campaign.Id, CancellationToken.None);
            Assert.Null(reloaded!.SettledAtUtc); // still desynced, as if the process crashed here
        }

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleCampaignBudgetCommandHandler(
                new AdCampaignRepository(context), new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var result = await handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var final = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.NotNull(final!.SettledAtUtc);

        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "AdvertisingCampaign" && e.SourceId == campaign.Id);
        Assert.Equal(1, entryCount);
    }

    /// <summary>
    /// Requirement G: HasSettlementEntryAsync filters strictly on EntryType=AdvertisingRevenue, so an
    /// unrelated entry under the same (SourceType, SourceId) but a different EntryType must never be misread
    /// as "the AdvertisingRevenue settlement already happened." Note: IFinancialLedgerRepository.PostBatchAsync's
    /// own up-front idempotency check is scoped to (SourceType, SourceId) only (coarser than the DB's unique
    /// (SourceType, SourceId, EntryType) index) — pre-existing, unmodified Payments behavior, not something this
    /// fix changed. In real Advertising usage a campaign's SourceId only ever receives AdvertisingRevenue
    /// entries, so this collision cannot occur naturally; this test forces it anyway to prove the safe,
    /// fail-closed outcome: the handler must NEVER silently mark the campaign Settled in this ambiguous state,
    /// and must never post a second entry.
    /// </summary>
    [Fact]
    public async Task Handle_UnrelatedLedgerEntryForSameSource_NeverFalselyMarksSettled()
    {
        var campaign = await CreateCompletedCampaignWithConsumedBudgetAsync(20m);

        await using (var context = _fixture.CreateContext())
        {
            var accountRepository = new FinancialAccountRepository(context);
            var ledgerRepository = new FinancialLedgerRepository(context);
            var platformAccount = await accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);

            var unrelatedLine = new LedgerPostingLine(
                platformAccount.Id, platformAccount.Id, 1m, LedgerEntryType.Adjustment, "Entrée sans rapport (test)",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, 1m)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, 1m)]);

            var posted = await ledgerRepository.PostBatchAsync(
                "AdvertisingCampaign", campaign.Id, "TND", null, DateTime.UtcNow, [unrelatedLine], CancellationToken.None);
            Assert.True(posted);
        }

        await using (var context = _fixture.CreateContext())
        {
            var billingService = new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
            Assert.False(await billingService.HasSettlementEntryAsync(campaign.Id, CancellationToken.None));

            var handler = new SettleCampaignBudgetCommandHandler(new AdCampaignRepository(context), billingService);
            var result = await handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.False(result.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Null(reloaded!.SettledAtUtc);

        var entries = await readContext.FinancialLedgerEntries
            .Where(e => e.SourceType == "AdvertisingCampaign" && e.SourceId == campaign.Id).ToListAsync();
        Assert.Single(entries);
        Assert.Equal(LedgerEntryType.Adjustment, entries[0].EntryType);
    }

    [Fact]
    public async Task Handle_FailureBeforeLedgerCommit_LeavesCampaignUnsettledAndRetryable()
    {
        var campaign = await CreateCompletedCampaignWithConsumedBudgetAsync(10m);

        // No ledger entry is ever posted for this campaign — simulates a failure occurring before
        // PostBatchAsync's own commit. The campaign must remain unsettled and the retry must succeed normally.
        await using var readContext = _fixture.CreateContext();
        var beforeRetry = await new AdCampaignRepository(readContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Null(beforeRetry!.SettledAtUtc);

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleCampaignBudgetCommandHandler(
                new AdCampaignRepository(context), new AdvertisingBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var result = await handler.Handle(new SettleCampaignBudgetCommand(campaign.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        // A fresh context, never one already reused for Handle() above — TryMarkSettledAsync's own
        // ExecuteUpdateAsync bypasses EF's change tracker, so re-querying through the SAME context/identity
        // map would silently return the stale pre-update tracked instance instead of the committed row.
        await using var finalReadContext = _fixture.CreateContext();
        var reloaded = await new AdCampaignRepository(finalReadContext).GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }
}
