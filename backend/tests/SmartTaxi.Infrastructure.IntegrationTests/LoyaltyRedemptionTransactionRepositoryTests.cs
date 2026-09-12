using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Loyalty.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against a real PostgreSQL database, the Module 7 audit fixes #3
/// (request-level redemption idempotency) and #4 (no compensation race —
/// debit, usage-limit reservation, and redemption persistence are one
/// transaction).
/// </summary>
[Collection("SharedPostgres")]
public class LoyaltyRedemptionTransactionRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public LoyaltyRedemptionTransactionRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<LoyaltyAccount> CreateAccountWithBalanceAsync(int rewardPoints)
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await using var seedContext = _fixture.CreateContext();
        await new LoyaltyAccountRepository(seedContext).TryAddAsync(account, CancellationToken.None);
        await new LoyaltyPointLedgerRepository(seedContext).TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, rewardPoints, "Payment", Guid.NewGuid(), "solde initial"),
            [], DateTime.UtcNow, CancellationToken.None);
        return account;
    }

    private async Task<LoyaltyReward> CreateRewardAsync(int cost, int? usageLimit = null)
    {
        var reward = LoyaltyReward.Create($"REWARD-{Guid.NewGuid():N}", "Reward", "desc", cost, LoyaltyRewardType.FreeService, [], null, null, usageLimit, DateTime.UtcNow);
        await using var context = _fixture.CreateContext();
        await new LoyaltyRewardRepository(context).AddAsync(reward, CancellationToken.None);
        return reward;
    }

    [Fact]
    public async Task TryRedeemAsync_TwoConcurrentAttemptsWithSameIdempotencyKey_OnlyOneDebit()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60);
        const string idempotencyKey = "same-key";

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyRedemptionTransactionRepository(context1).TryRedeemAsync(account.UserId, account.Id, reward.Id, 60, idempotencyKey, "échange", DateTime.UtcNow, CancellationToken.None),
            new LoyaltyRedemptionTransactionRepository(context2).TryRedeemAsync(account.UserId, account.Id, reward.Id, 60, idempotencyKey, "échange", DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r.Outcome == LoyaltyRedemptionAttemptOutcome.Created);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(40, reloaded!.CurrentRewardPoints);
    }

    [Fact]
    public async Task TryRedeemAsync_RetryWithSameIdempotencyKeyAfterSuccess_ReplaysWithoutSecondDebit()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60);

        await using var context = _fixture.CreateContext();
        var repository = new LoyaltyRedemptionTransactionRepository(context);

        var first = await repository.TryRedeemAsync(account.UserId, account.Id, reward.Id, 60, "same-key", "échange", DateTime.UtcNow, CancellationToken.None);
        var second = await repository.TryRedeemAsync(account.UserId, account.Id, reward.Id, 60, "same-key", "échange", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(LoyaltyRedemptionAttemptOutcome.Created, first.Outcome);
        Assert.Equal(LoyaltyRedemptionAttemptOutcome.Replayed, second.Outcome);
        Assert.Equal(first.Redemption!.Id, second.Redemption!.Id);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(40, reloaded!.CurrentRewardPoints);
    }

    [Fact]
    public async Task TryRedeemAsync_UsageLimitAlreadyReached_RollsBackDebitEntirelyWithNoCompensation()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60, usageLimit: 1);

        await using (var exhaustContext = _fixture.CreateContext())
        {
            await new LoyaltyRewardRepository(exhaustContext).TryIncrementRedeemedCountAsync(reward.Id, DateTime.UtcNow, CancellationToken.None);
        }

        await using var context = _fixture.CreateContext();
        var attempt = await new LoyaltyRedemptionTransactionRepository(context).TryRedeemAsync(
            account.UserId, account.Id, reward.Id, 60, "key-1", "échange", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(LoyaltyRedemptionAttemptOutcome.UsageLimitReached, attempt.Outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedAccount = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        var reloadedReward = await new LoyaltyRewardRepository(readContext).GetByIdAsync(reward.Id, CancellationToken.None);

        // The debit never actually landed — the whole transaction rolled back, so there is nothing to
        // compensate and no redemption row was ever created for this failed attempt.
        Assert.Equal(100, reloadedAccount!.CurrentRewardPoints);
        Assert.Equal(1, reloadedReward!.RedeemedCount);
    }

    [Fact]
    public async Task TryRedeemAsync_TwoConcurrentRedemptionsAgainstLastUsageLimitSlot_OnlyOneSucceedsAndBalanceReflectsOnlyThatOne()
    {
        // Balance is large enough that it is never the bottleneck — the usage limit (1 slot) is.
        var account = await CreateAccountWithBalanceAsync(1000);
        var reward = await CreateRewardAsync(cost: 60, usageLimit: 1);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyRedemptionTransactionRepository(context1).TryRedeemAsync(account.UserId, account.Id, reward.Id, 60, "key-a", "échange", DateTime.UtcNow, CancellationToken.None),
            new LoyaltyRedemptionTransactionRepository(context2).TryRedeemAsync(account.UserId, account.Id, reward.Id, 60, "key-b", "échange", DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r.Outcome == LoyaltyRedemptionAttemptOutcome.Created);
        Assert.Single(results, r => r.Outcome == LoyaltyRedemptionAttemptOutcome.UsageLimitReached);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(940, reloaded!.CurrentRewardPoints);
    }
}
