using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that FinancialLedgerRepository's
/// atomicity, idempotency, and concurrency guarantees actually hold — this is
/// the piece the Application-layer LedgerPostingTests can't verify, since
/// those run against FakeFinancialLedgerRepository (an in-memory dictionary
/// with no real transaction or unique-index semantics).
///
/// The Platform account is a true singleton (one row for the whole database,
/// shared with every other test in this assembly that posts a payment or
/// refund — by design, per FinancialAccount's own doc comment). Every
/// assertion against it here therefore compares a before/after delta, never
/// an absolute balance — an absolute assertion would be a race against every
/// other test touching Platform under the shared "SharedPostgres" collection.
/// </summary>
[Collection("SharedPostgres")]
public class FinancialLedgerRepositoryTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly SharedPostgresFixture _fixture;

    public FinancialLedgerRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid PlatformAccountId, Guid DriverAccountId)> SeedAccountsAsync(Guid driverId)
    {
        await using var context = _fixture.CreateContext();
        var repository = new FinancialAccountRepository(context);
        var platform = await repository.GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
        var driver = await repository.GetOrCreateAsync(FinancialAccountType.Driver, driverId, "TND", CancellationToken.None);
        return (platform.Id, driver.Id);
    }

    private async Task<FinancialAccount> ReadAccountAsync(Guid accountId)
    {
        await using var context = _fixture.CreateContext();
        return await context.FinancialAccounts.SingleAsync(a => a.Id == accountId);
    }

    [Fact]
    public async Task PostBatchAsync_PostsEntriesAndAppliesBalanceEffectsToTheRightAccountsAndBuckets()
    {
        var (platformId, driverId) = await SeedAccountsAsync(Guid.NewGuid());
        var platformPendingBefore = (await ReadAccountAsync(platformId)).PendingBalance;
        var sourceId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var lines = new List<LedgerPostingLine>
        {
            new(platformId, platformId, 100m, LedgerEntryType.PaymentCollected, "Encaissement",
                DebitEffects: [], CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, 100m)]),
            new(platformId, driverId, 80m, LedgerEntryType.DriverEarning, "Gain chauffeur",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, -80m)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, 80m)])
        };

        await using (var writeContext = _fixture.CreateContext())
        {
            var posted = await new FinancialLedgerRepository(writeContext)
                .PostBatchAsync("Payment", sourceId, "TND", AdminUserId, utcNow, lines, CancellationToken.None);
            Assert.True(posted);
        }

        await using var readContext = _fixture.CreateContext();
        var entries = await readContext.FinancialLedgerEntries.Where(e => e.SourceType == "Payment" && e.SourceId == sourceId).ToListAsync();
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.TransactionNumber)));

        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformId);
        var driver = await readContext.FinancialAccounts.SingleAsync(a => a.Id == driverId);

        Assert.Equal(20m, platformAfter.PendingBalance - platformPendingBefore);
        Assert.Equal(80m, driver.AvailableBalance);
    }

    [Fact]
    public async Task PostBatchAsync_CalledTwiceForSameSource_SecondCallIsNoOpAndBalanceIsAppliedOnlyOnce()
    {
        var (platformId, _) = await SeedAccountsAsync(Guid.NewGuid());
        var platformPendingBefore = (await ReadAccountAsync(platformId)).PendingBalance;
        var sourceId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var lines = new List<LedgerPostingLine>
        {
            new(platformId, platformId, 50m, LedgerEntryType.PaymentCollected, "Encaissement",
                DebitEffects: [], CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, 50m)])
        };

        await using (var contextOne = _fixture.CreateContext())
        {
            var firstPost = await new FinancialLedgerRepository(contextOne)
                .PostBatchAsync("Payment", sourceId, "TND", AdminUserId, utcNow, lines, CancellationToken.None);
            Assert.True(firstPost);
        }

        await using (var contextTwo = _fixture.CreateContext())
        {
            var secondPost = await new FinancialLedgerRepository(contextTwo)
                .PostBatchAsync("Payment", sourceId, "TND", AdminUserId, utcNow, lines, CancellationToken.None);
            Assert.False(secondPost);
        }

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries.CountAsync(e => e.SourceType == "Payment" && e.SourceId == sourceId);
        Assert.Equal(1, entryCount);

        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformId);
        Assert.Equal(50m, platformAfter.PendingBalance - platformPendingBefore);
    }

    [Fact]
    public async Task ConcurrentPostBatchAsync_ForSameSource_OnlyOneSucceedsAndBalanceReflectsExactlyOnePosting()
    {
        var (platformId, _) = await SeedAccountsAsync(Guid.NewGuid());
        var platformAvailableBefore = (await ReadAccountAsync(platformId)).AvailableBalance;
        var sourceId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var lines = new List<LedgerPostingLine>
        {
            new(platformId, platformId, 30m, LedgerEntryType.Refund, "Remboursement",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, -30m)], CreditEffects: [])
        };

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new FinancialLedgerRepository(contextA).PostBatchAsync("Refund", sourceId, "TND", AdminUserId, utcNow, lines, CancellationToken.None),
            new FinancialLedgerRepository(contextB).PostBatchAsync("Refund", sourceId, "TND", AdminUserId, utcNow, lines, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries.CountAsync(e => e.SourceType == "Refund" && e.SourceId == sourceId);
        Assert.Equal(1, entryCount);

        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformId);
        Assert.Equal(-30m, platformAfter.AvailableBalance - platformAvailableBefore);
    }

    [Fact]
    public async Task ConcurrentPostBatchAsync_ForDifferentSourcesOnSameAccount_BothEffectsAreAppliedWithoutLostUpdates()
    {
        var (platformId, _) = await SeedAccountsAsync(Guid.NewGuid());
        var platformAvailableBefore = (await ReadAccountAsync(platformId)).AvailableBalance;
        var sourceIdOne = Guid.NewGuid();
        var sourceIdTwo = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        List<LedgerPostingLine> LinesFor(decimal amount) =>
        [
            new(platformId, platformId, amount, LedgerEntryType.PlatformCommission, "Commission",
                DebitEffects: [], CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, amount)])
        ];

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new FinancialLedgerRepository(contextA).PostBatchAsync("Payment", sourceIdOne, "TND", AdminUserId, utcNow, LinesFor(15m), CancellationToken.None),
            new FinancialLedgerRepository(contextB).PostBatchAsync("Payment", sourceIdTwo, "TND", AdminUserId, utcNow, LinesFor(25m), CancellationToken.None));

        Assert.All(results, Assert.True);

        await using var readContext = _fixture.CreateContext();
        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformId);
        Assert.Equal(40m, platformAfter.AvailableBalance - platformAvailableBefore);
    }
}
