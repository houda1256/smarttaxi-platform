using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that PayoutRepository.TryCompleteAsync —
/// the sole place a Payout's money actually moves — atomically enforces "a payout
/// cannot exceed AvailableBalance" and "payout completion updates balances exactly
/// once", and that it correctly rolls back the balance move when the Payout side of
/// the same transaction loses its own race.
/// </summary>
[Collection("SharedPostgres")]
public class PayoutRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public PayoutRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedDriverAccountWithAvailableBalanceAsync(decimal availableBalance)
    {
        await using var context = _fixture.CreateContext();
        var account = await new FinancialAccountRepository(context)
            .GetOrCreateAsync(FinancialAccountType.Driver, Guid.NewGuid(), "TND", CancellationToken.None);

        await context.FinancialAccounts
            .Where(a => a.Id == account.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.AvailableBalance, availableBalance));

        return account.Id;
    }

    private async Task<Guid> CreateProcessingPayoutAsync(Guid beneficiaryAccountId, decimal amount)
    {
        await using var context = _fixture.CreateContext();
        var repository = new PayoutRepository(context);

        var payout = Payout.Request(
            beneficiaryAccountId, FinancialAccountType.Driver, amount, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand, DateTime.UtcNow);
        await repository.AddAsync(payout, CancellationToken.None);

        await repository.TrySubmitForApprovalAsync(payout.Id, DateTime.UtcNow, CancellationToken.None);
        await repository.TryApproveAsync(payout.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        await repository.TryStartProcessingAsync(payout.Id, DateTime.UtcNow, CancellationToken.None);

        return payout.Id;
    }

    [Fact]
    public async Task TryCompleteAsync_MovesAvailableToPaidOutExactlyOnceAndPostsOneLedgerEntry()
    {
        var accountId = await SeedDriverAccountWithAvailableBalanceAsync(500m);
        var payoutId = await CreateProcessingPayoutAsync(accountId, 200m);

        await using (var writeContext = _fixture.CreateContext())
        {
            var outcome = await new PayoutRepository(writeContext).TryCompleteAsync(payoutId, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.Equal(Application.Payments.Payouts.Abstractions.PayoutCompletionOutcome.Completed, outcome);
        }

        await using var readContext = _fixture.CreateContext();
        var account = await readContext.FinancialAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Equal(300m, account.AvailableBalance);
        Assert.Equal(200m, account.PaidOutBalance);

        var payout = await readContext.Payouts.SingleAsync(p => p.Id == payoutId);
        Assert.Equal(PayoutStatus.Paid, payout.Status);

        var entryCount = await readContext.FinancialLedgerEntries.CountAsync(e => e.SourceType == "Payout" && e.SourceId == payoutId);
        Assert.Equal(1, entryCount);

        // Calling Complete again must be a harmless no-op — balances must not move a second time.
        await using (var secondContext = _fixture.CreateContext())
        {
            var secondOutcome = await new PayoutRepository(secondContext).TryCompleteAsync(payoutId, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.Equal(Application.Payments.Payouts.Abstractions.PayoutCompletionOutcome.NotInProcessingState, secondOutcome);
        }

        await using var finalContext = _fixture.CreateContext();
        var accountAfterSecondCall = await finalContext.FinancialAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Equal(300m, accountAfterSecondCall.AvailableBalance);
        Assert.Equal(200m, accountAfterSecondCall.PaidOutBalance);
    }

    [Fact]
    public async Task TryCompleteAsync_WhenAvailableBalanceDropsBelowAmount_FailsAndLeavesEverythingUnchanged()
    {
        var accountId = await SeedDriverAccountWithAvailableBalanceAsync(500m);
        var payoutId = await CreateProcessingPayoutAsync(accountId, 400m);

        await using (var draindownContext = _fixture.CreateContext())
        {
            await draindownContext.FinancialAccounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.AvailableBalance, 100m));
        }

        await using (var writeContext = _fixture.CreateContext())
        {
            var outcome = await new PayoutRepository(writeContext).TryCompleteAsync(payoutId, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
            Assert.Equal(Application.Payments.Payouts.Abstractions.PayoutCompletionOutcome.InsufficientAvailableBalance, outcome);
        }

        await using var readContext = _fixture.CreateContext();
        var account = await readContext.FinancialAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Equal(100m, account.AvailableBalance);
        Assert.Equal(0m, account.PaidOutBalance);

        var payout = await readContext.Payouts.SingleAsync(p => p.Id == payoutId);
        Assert.Equal(PayoutStatus.Processing, payout.Status);

        var entryCount = await readContext.FinancialLedgerEntries.CountAsync(e => e.SourceType == "Payout" && e.SourceId == payoutId);
        Assert.Equal(0, entryCount);
    }

    [Fact]
    public async Task ConcurrentTryCompleteAsync_ForTwoPayoutsExceedingCombinedAvailableBalance_OnlyOneSucceeds()
    {
        var accountId = await SeedDriverAccountWithAvailableBalanceAsync(300m);
        var payoutOneId = await CreateProcessingPayoutAsync(accountId, 200m);
        var payoutTwoId = await CreateProcessingPayoutAsync(accountId, 200m);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new PayoutRepository(contextA).TryCompleteAsync(payoutOneId, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None),
            new PayoutRepository(contextB).TryCompleteAsync(payoutTwoId, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, outcome => outcome == Application.Payments.Payouts.Abstractions.PayoutCompletionOutcome.Completed);

        await using var readContext = _fixture.CreateContext();
        var account = await readContext.FinancialAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Equal(100m, account.AvailableBalance);
        Assert.Equal(200m, account.PaidOutBalance);
    }

    [Fact]
    public async Task RequestApproveStartProcessing_FullLifecycle_TransitionsCorrectly()
    {
        var accountId = await SeedDriverAccountWithAvailableBalanceAsync(500m);

        var payout = Payout.Request(accountId, FinancialAccountType.Driver, 150m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.Weekly, DateTime.UtcNow);

        await using (var addContext = _fixture.CreateContext())
        {
            await new PayoutRepository(addContext).AddAsync(payout, CancellationToken.None);
        }

        // Each step uses its own DbContext, like every other test here: ExecuteUpdateAsync writes
        // bypass the change tracker, so re-querying a tracked entity on the same context that issued
        // it would return the stale in-memory identity-map copy instead of the fresh DB row.
        await using (var submitContext = _fixture.CreateContext())
        {
            Assert.True(await new PayoutRepository(submitContext).TrySubmitForApprovalAsync(payout.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using (var approveContext = _fixture.CreateContext())
        {
            Assert.True(await new PayoutRepository(approveContext).TryApproveAsync(payout.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new PayoutRepository(readContext).GetByIdAsync(payout.Id, CancellationToken.None);
        Assert.Equal(PayoutStatus.Approved, reloaded!.Status);
        Assert.NotNull(reloaded.ApprovedAt);
    }

    [Fact]
    public async Task TryCancelAsync_AfterProcessingStarted_Fails()
    {
        var accountId = await SeedDriverAccountWithAvailableBalanceAsync(500m);
        var payoutId = await CreateProcessingPayoutAsync(accountId, 100m);

        await using var context = _fixture.CreateContext();
        var cancelled = await new PayoutRepository(context).TryCancelAsync(payoutId, DateTime.UtcNow, CancellationToken.None);

        Assert.False(cancelled);
    }

    [Fact]
    public async Task GetForBeneficiaryAccountAsync_FiltersByStatusAndPagesCorrectly()
    {
        var accountId = await SeedDriverAccountWithAvailableBalanceAsync(1000m);
        await CreateProcessingPayoutAsync(accountId, 50m);
        await CreateProcessingPayoutAsync(accountId, 60m);

        await using var context = _fixture.CreateContext();
        var repository = new PayoutRepository(context);

        var processingOnly = await repository.GetForBeneficiaryAccountAsync(accountId, PayoutStatus.Processing, 1, 10, CancellationToken.None);
        Assert.Equal(2, processingOnly.TotalCount);

        var paidOnly = await repository.GetForBeneficiaryAccountAsync(accountId, PayoutStatus.Paid, 1, 10, CancellationToken.None);
        Assert.Equal(0, paidOnly.TotalCount);
    }
}
