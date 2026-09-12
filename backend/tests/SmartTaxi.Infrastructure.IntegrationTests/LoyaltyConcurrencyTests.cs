using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Loyalty.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against a real PostgreSQL database, every concurrency guarantee
/// Module 7 depends on: duplicate-award rejection, balance-guarded debits,
/// referral-reward exactly-once, expiration exactly-once, challenge-reward
/// exactly-once, and reward-catalog stock limits. All follow the same
/// Task.WhenAll + SharedPostgresFixture pattern as
/// SingleUseTokenConcurrencyTests/SubscriptionRepositoryTests.
/// </summary>
[Collection("SharedPostgres")]
public class LoyaltyConcurrencyTests
{
    private readonly SharedPostgresFixture _fixture;

    public LoyaltyConcurrencyTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<LoyaltyAccount> CreateAccountAsync()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await using var context = _fixture.CreateContext();
        await new LoyaltyAccountRepository(context).TryAddAsync(account, CancellationToken.None);
        return account;
    }

    [Fact]
    public async Task TryCreditAsync_TwoConcurrentAwardsForSamePayment_OnlyOneSucceeds()
    {
        var account = await CreateAccountAsync();
        var paymentId = Guid.NewGuid();
        var request = new LoyaltyLedgerAppendRequest(
            account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 20, "Payment", paymentId, "Trajet payé");

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyPointLedgerRepository(context1).TryCreditAsync(request, [], DateTime.UtcNow, CancellationToken.None),
            new LoyaltyPointLedgerRepository(context2).TryCreditAsync(request, [], DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r is not null);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(20, reloaded!.CurrentRewardPoints);
    }

    [Fact]
    public async Task TryDebitAsync_TwoConcurrentRedemptionsAgainstBalanceEnoughForOnlyOne_OnlyOneSucceedsAndBalanceNeverGoesNegative()
    {
        var account = await CreateAccountAsync();

        await using (var seedContext = _fixture.CreateContext())
        {
            await new LoyaltyPointLedgerRepository(seedContext).TryCreditAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "Solde initial"),
                [], DateTime.UtcNow, CancellationToken.None);
        }

        var request1 = new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 60, "Redemption", Guid.NewGuid(), "Échange 1");
        var request2 = new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 60, "Redemption", Guid.NewGuid(), "Échange 2");

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyPointLedgerRepository(context1).TryDebitAsync(request1, DateTime.UtcNow, CancellationToken.None),
            new LoyaltyPointLedgerRepository(context2).TryDebitAsync(request2, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r is not null);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(40, reloaded!.CurrentRewardPoints);
        Assert.True(reloaded.CurrentRewardPoints >= 0);
    }

    [Fact]
    public async Task LoyaltyReferralRewardRepository_TwoConcurrentGrantsForSameReferral_OnlyOneSucceeds()
    {
        var referralId = Guid.NewGuid();
        var first = LoyaltyReferralReward.Grant(referralId, Guid.NewGuid(), Guid.NewGuid(), 100, 50, "referral.default", DateTime.UtcNow);
        var second = LoyaltyReferralReward.Grant(referralId, Guid.NewGuid(), Guid.NewGuid(), 100, 50, "referral.default", DateTime.UtcNow);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyReferralRewardRepository(context1).TryAddAsync(first, CancellationToken.None),
            new LoyaltyReferralRewardRepository(context2).TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, r => r);
    }

    [Fact]
    public async Task TryDebitAsync_TwoConcurrentExpirationsOfSameEarnEntry_OnlyOneSucceeds()
    {
        var account = await CreateAccountAsync();
        LoyaltyPointLedgerEntry earnEntry;

        await using (var seedContext = _fixture.CreateContext())
        {
            earnEntry = (await new LoyaltyPointLedgerRepository(seedContext).TryCreditAsync(
                new LoyaltyLedgerAppendRequest(
                    account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(),
                    "Trajet payé", ExpirationAtUtc: DateTime.UtcNow.AddDays(-1)),
                [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None))!;
        }

        var expireRequest = new LoyaltyLedgerAppendRequest(
            account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire, 100, "LoyaltyPointLedgerEntry", earnEntry.Id,
            "Expiration des points");

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyPointLedgerRepository(context1).TryDebitAsync(expireRequest, DateTime.UtcNow, CancellationToken.None),
            new LoyaltyPointLedgerRepository(context2).TryDebitAsync(expireRequest, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r is not null);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(0, reloaded!.CurrentRewardPoints);
    }

    [Fact]
    public async Task TryCreditAsync_TwoConcurrentChallengeRewardsForSameProgress_OnlyOneSucceeds()
    {
        var account = await CreateAccountAsync();
        var progressId = Guid.NewGuid();

        var request = new LoyaltyLedgerAppendRequest(
            account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ChallengeReward, 50, "Challenge", progressId,
            "Défi complété");

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyPointLedgerRepository(context1).TryCreditAsync(request, [], DateTime.UtcNow, CancellationToken.None),
            new LoyaltyPointLedgerRepository(context2).TryCreditAsync(request, [], DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r is not null);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(50, reloaded!.CurrentRewardPoints);
    }

    [Fact]
    public async Task LoyaltyRewardRepository_TwoConcurrentRedemptionsOfLastUnitOfLimitedStock_OnlyOneSucceeds()
    {
        var reward = LoyaltyReward.Create("LIMITED", "Offre limitée", "desc", 50, LoyaltyRewardType.FreeService, [], null, null, 1, DateTime.UtcNow);

        await using (var seedContext = _fixture.CreateContext())
        {
            await new LoyaltyRewardRepository(seedContext).AddAsync(reward, CancellationToken.None);
        }

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyRewardRepository(context1).TryIncrementRedeemedCountAsync(reward.Id, DateTime.UtcNow, CancellationToken.None),
            new LoyaltyRewardRepository(context2).TryIncrementRedeemedCountAsync(reward.Id, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyRewardRepository(readContext).GetByIdAsync(reward.Id, CancellationToken.None);
        Assert.Equal(1, reloaded!.RedeemedCount);
    }
}
