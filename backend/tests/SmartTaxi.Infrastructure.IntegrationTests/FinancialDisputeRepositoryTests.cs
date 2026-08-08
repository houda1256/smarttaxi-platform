using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that opening a FinancialDispute
/// atomically reserves DisputedAmount (Available -> Reserved) on the Platform
/// account, and that resolution correctly releases or permanently adjusts it —
/// the exact "disputed amounts move into ReservedBalance" / "dispute
/// resolution releases or adjusts the correct amount" requirements.
/// </summary>
[Collection("SharedPostgres")]
public class FinancialDisputeRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public FinancialDisputeRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Returns the Platform account's state right after crediting it with a large amount of
    /// AvailableBalance — GetOrCreateAsync alone returns a zero-balance account (or whatever residual
    /// balance other tests' fully-round-tripped deltas happened to leave), which is never enough to
    /// satisfy TryOpenAsync's own "AvailableBalance >= DisputedAmount" guard. The credit is additive
    /// (ExecuteUpdateAsync increment, not an absolute set), so it never corrupts other tests sharing
    /// this same singleton row — every assertion in this file then compares against this post-credit
    /// snapshot as "before", never an absolute value.
    /// </summary>
    private async Task<Domain.Payments.Accounts.Entities.FinancialAccount> GetOrCreatePlatformAccountAsync()
    {
        Guid accountId;

        await using (var createContext = _fixture.CreateContext())
        {
            var account = await new FinancialAccountRepository(createContext).GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
            accountId = account.Id;
        }

        await using (var creditContext = _fixture.CreateContext())
        {
            await creditContext.FinancialAccounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.AvailableBalance, a => a.AvailableBalance + 10000m));
        }

        // A fresh context for the final read: ExecuteUpdateAsync bypasses the change tracker, so reading
        // back on a context that already has this row in its identity map (e.g. the one that ran
        // GetOrCreateAsync) would return the stale pre-credit instance instead of the fresh DB row.
        await using var readContext = _fixture.CreateContext();
        return await readContext.FinancialAccounts.SingleAsync(a => a.Id == accountId);
    }

    private static FinancialDispute NewPaymentDispute(decimal amount) => FinancialDispute.Open(
        FinancialDisputeCategory.IncorrectFare, Guid.NewGuid(), null, null, amount, "TND", "Tarif contesté", null, Guid.NewGuid(), DateTime.UtcNow);

    [Fact]
    public async Task TryOpenAsync_MovesDisputedAmountFromAvailableToReserved()
    {
        var platformBefore = await GetOrCreatePlatformAccountAsync();
        var dispute = NewPaymentDispute(150m);

        await using (var writeContext = _fixture.CreateContext())
        {
            var opened = await new FinancialDisputeRepository(writeContext).TryOpenAsync(dispute, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.True(opened);
        }

        await using var readContext = _fixture.CreateContext();
        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformBefore.Id);

        Assert.Equal(-150m, platformAfter.AvailableBalance - platformBefore.AvailableBalance);
        Assert.Equal(150m, platformAfter.ReservedBalance - platformBefore.ReservedBalance);

        var reloaded = await new FinancialDisputeRepository(readContext).GetByIdAsync(dispute.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(FinancialDisputeStatus.Open, reloaded!.Status);
    }

    [Fact]
    public async Task ResolveWithRelease_ReturnsReservedAmountToAvailable()
    {
        var platformBefore = await GetOrCreatePlatformAccountAsync();
        var dispute = NewPaymentDispute(200m);

        await using (var openContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(openContext).TryOpenAsync(dispute, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using (var reviewContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(reviewContext).TryStartReviewAsync(dispute.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using (var resolveContext = _fixture.CreateContext())
        {
            var resolved = await new FinancialDisputeRepository(resolveContext).TryResolveAsync(
                dispute.Id, "Remboursement confirmé", DisputeResolutionOutcome.Release, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.True(resolved);
        }

        await using var readContext = _fixture.CreateContext();
        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformBefore.Id);

        Assert.Equal(0m, platformAfter.AvailableBalance - platformBefore.AvailableBalance);
        Assert.Equal(0m, platformAfter.ReservedBalance - platformBefore.ReservedBalance);

        var reloaded = await new FinancialDisputeRepository(readContext).GetByIdAsync(dispute.Id, CancellationToken.None);
        Assert.Equal(FinancialDisputeStatus.Resolved, reloaded!.Status);
        Assert.NotNull(reloaded.ResolvedAt);
    }

    [Fact]
    public async Task ResolveWithAdjust_PermanentlyRemovesAmountFromReserved()
    {
        var platformBefore = await GetOrCreatePlatformAccountAsync();
        var dispute = NewPaymentDispute(120m);

        await using (var openContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(openContext).TryOpenAsync(dispute, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using (var reviewContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(reviewContext).TryStartReviewAsync(dispute.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using (var resolveContext = _fixture.CreateContext())
        {
            var resolved = await new FinancialDisputeRepository(resolveContext).TryResolveAsync(
                dispute.Id, "Montant ajusté définitivement", DisputeResolutionOutcome.Adjust, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.True(resolved);
        }

        await using var readContext = _fixture.CreateContext();
        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformBefore.Id);

        Assert.Equal(-120m, platformAfter.AvailableBalance - platformBefore.AvailableBalance);
        Assert.Equal(0m, platformAfter.ReservedBalance - platformBefore.ReservedBalance);
    }

    [Fact]
    public async Task TryResolveAsync_WithoutStartingReviewFirst_Fails()
    {
        var dispute = NewPaymentDispute(80m);

        await using (var openContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(openContext).TryOpenAsync(dispute, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using var resolveContext = _fixture.CreateContext();
        var resolved = await new FinancialDisputeRepository(resolveContext).TryResolveAsync(
            dispute.Id, "Résolution", DisputeResolutionOutcome.Release, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        Assert.False(resolved);
    }

    [Fact]
    public async Task Reject_AlwaysReleasesTheReservation()
    {
        var platformBefore = await GetOrCreatePlatformAccountAsync();
        var dispute = NewPaymentDispute(90m);

        await using (var openContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(openContext).TryOpenAsync(dispute, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using (var reviewContext = _fixture.CreateContext())
        {
            await new FinancialDisputeRepository(reviewContext).TryStartReviewAsync(dispute.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using (var rejectContext = _fixture.CreateContext())
        {
            var rejected = await new FinancialDisputeRepository(rejectContext).TryRejectAsync(
                dispute.Id, "Litige non fondé", Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.True(rejected);
        }

        await using var readContext = _fixture.CreateContext();
        var platformAfter = await readContext.FinancialAccounts.SingleAsync(a => a.Id == platformBefore.Id);

        Assert.Equal(0m, platformAfter.AvailableBalance - platformBefore.AvailableBalance);
        Assert.Equal(0m, platformAfter.ReservedBalance - platformBefore.ReservedBalance);
    }

    [Fact]
    public async Task ConcurrentTryOpenAsync_ForTwoDisputesExceedingCombinedAvailableBalance_OnlyOneSucceeds()
    {
        // Reserves against a Payout's beneficiary account (a fresh, per-test Driver account) rather than the
        // Platform singleton — pinning Platform's balance here would corrupt every other test in this shared-
        // Postgres suite that also touches Platform, since tests in the collection run sequentially but share
        // the same rows.
        Guid beneficiaryAccountId;
        Guid payoutId;

        await using (var seedContext = _fixture.CreateContext())
        {
            var account = await new FinancialAccountRepository(seedContext)
                .GetOrCreateAsync(FinancialAccountType.Driver, Guid.NewGuid(), "TND", CancellationToken.None);
            beneficiaryAccountId = account.Id;

            await seedContext.FinancialAccounts
                .Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.AvailableBalance, 100m));

            var payout = Payout.Request(account.Id, FinancialAccountType.Driver, 100m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand, DateTime.UtcNow);
            await new PayoutRepository(seedContext).AddAsync(payout, CancellationToken.None);
            payoutId = payout.Id;
        }

        var disputeA = FinancialDispute.Open(
            FinancialDisputeCategory.MissingPayout, null, null, payoutId, 80m, "TND", "Versement manquant A", null, Guid.NewGuid(), DateTime.UtcNow);
        var disputeB = FinancialDispute.Open(
            FinancialDisputeCategory.MissingPayout, null, null, payoutId, 80m, "TND", "Versement manquant B", null, Guid.NewGuid(), DateTime.UtcNow);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new FinancialDisputeRepository(contextA).TryOpenAsync(disputeA, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None),
            new FinancialDisputeRepository(contextB).TryOpenAsync(disputeB, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var account2 = await readContext.FinancialAccounts.SingleAsync(a => a.Id == beneficiaryAccountId);
        Assert.Equal(20m, account2.AvailableBalance);
        Assert.Equal(80m, account2.ReservedBalance);
    }
}
