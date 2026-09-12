using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Loyalty.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against a real PostgreSQL database, the Module 7 audit fix #1
/// guarantee: the LoyaltyReferralReward row and both parties' credits are one
/// all-or-nothing transaction. A failure anywhere in the sequence must leave
/// neither party credited and no reward row behind, and the referral must
/// remain retryable afterward.
/// </summary>
[Collection("SharedPostgres")]
public class LoyaltyReferralGrantRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public LoyaltyReferralGrantRepositoryTests(SharedPostgresFixture fixture)
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

    private static LoyaltyLedgerAppendRequest CreditRequest(LoyaltyAccount account, Guid referralId, int points) =>
        new(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ReferralReward, points, "Referral",
            referralId, "Récompense de parrainage", ReferralId: referralId);

    [Fact]
    public async Task TryGrantAsync_HappyPath_CreditsBothPartiesAndPersistsRewardRow()
    {
        var referrer = await CreateAccountAsync();
        var referee = await CreateAccountAsync();
        var referralId = Guid.NewGuid();
        var reward = LoyaltyReferralReward.Grant(referralId, referrer.UserId, referee.UserId, 200, 100, "referral.default", DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        var granted = await new LoyaltyReferralGrantRepository(context).TryGrantAsync(
            reward, CreditRequest(referrer, referralId, 200), CreditRequest(referee, referralId, 100), DateTime.UtcNow, CancellationToken.None);

        Assert.True(granted);

        await using var readContext = _fixture.CreateContext();
        var accountRepository = new LoyaltyAccountRepository(readContext);
        var reloadedReferrer = await accountRepository.GetByIdAsync(referrer.Id, CancellationToken.None);
        var reloadedReferee = await accountRepository.GetByIdAsync(referee.Id, CancellationToken.None);
        var rewardRow = await new LoyaltyReferralRewardRepository(readContext).GetByReferralIdAsync(referralId, CancellationToken.None);

        Assert.Equal(200, reloadedReferrer!.CurrentRewardPoints);
        Assert.Equal(100, reloadedReferee!.CurrentRewardPoints);
        Assert.NotNull(rewardRow);
    }

    [Fact]
    public async Task TryGrantAsync_RefereeAccountMissingMidFlight_RollsBackEverythingIncludingReferrerCredit()
    {
        var referrer = await CreateAccountAsync();
        var referee = await CreateAccountAsync();
        var referralId = Guid.NewGuid();
        var reward = LoyaltyReferralReward.Grant(referralId, referrer.UserId, referee.UserId, 200, 100, "referral.default", DateTime.UtcNow);

        // Simulate a mid-flight fault on the referee's credit: its target account row disappears
        // between account resolution and the grant call, so its conditional UPDATE affects 0 rows.
        await using (var deleteContext = _fixture.CreateContext())
        {
            deleteContext.LoyaltyAccounts.Remove((await deleteContext.LoyaltyAccounts.FindAsync(referee.Id))!);
            await deleteContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        var granted = await new LoyaltyReferralGrantRepository(context).TryGrantAsync(
            reward, CreditRequest(referrer, referralId, 200), CreditRequest(referee, referralId, 100), DateTime.UtcNow, CancellationToken.None);

        Assert.False(granted);

        await using var readContext = _fixture.CreateContext();
        var reloadedReferrer = await new LoyaltyAccountRepository(readContext).GetByIdAsync(referrer.Id, CancellationToken.None);
        var rewardRow = await new LoyaltyReferralRewardRepository(readContext).GetByReferralIdAsync(referralId, CancellationToken.None);

        // The referrer's credit ran first and would have succeeded in isolation — proving the whole
        // transaction rolled back, not just the failing half.
        Assert.Equal(0, reloadedReferrer!.CurrentRewardPoints);
        Assert.Null(rewardRow);
    }

    [Fact]
    public async Task TryGrantAsync_RetryAfterFailureIsRestored_SucceedsNormally()
    {
        var referrer = await CreateAccountAsync();
        var referee = await CreateAccountAsync();
        var referralId = Guid.NewGuid();
        var failingReward = LoyaltyReferralReward.Grant(referralId, referrer.UserId, referee.UserId, 200, 100, "referral.default", DateTime.UtcNow);

        await using (var deleteContext = _fixture.CreateContext())
        {
            deleteContext.LoyaltyAccounts.Remove((await deleteContext.LoyaltyAccounts.FindAsync(referee.Id))!);
            await deleteContext.SaveChangesAsync();
        }

        await using (var failContext = _fixture.CreateContext())
        {
            var firstAttempt = await new LoyaltyReferralGrantRepository(failContext).TryGrantAsync(
                failingReward, CreditRequest(referrer, referralId, 200), CreditRequest(referee, referralId, 100), DateTime.UtcNow,
                CancellationToken.None);
            Assert.False(firstAttempt);
        }

        // Restore the referee's account (e.g. the underlying cause is fixed) and retry with a fresh reward instance.
        await using (var restoreContext = _fixture.CreateContext())
        {
            await new LoyaltyAccountRepository(restoreContext).TryAddAsync(referee, CancellationToken.None);
        }

        var retryReward = LoyaltyReferralReward.Grant(referralId, referrer.UserId, referee.UserId, 200, 100, "referral.default", DateTime.UtcNow);

        await using var retryContext = _fixture.CreateContext();
        var retryGranted = await new LoyaltyReferralGrantRepository(retryContext).TryGrantAsync(
            retryReward, CreditRequest(referrer, referralId, 200), CreditRequest(referee, referralId, 100), DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(retryGranted);

        await using var readContext = _fixture.CreateContext();
        var accountRepository = new LoyaltyAccountRepository(readContext);
        Assert.Equal(200, (await accountRepository.GetByIdAsync(referrer.Id, CancellationToken.None))!.CurrentRewardPoints);
        Assert.Equal(100, (await accountRepository.GetByIdAsync(referee.Id, CancellationToken.None))!.CurrentRewardPoints);
    }

    [Fact]
    public async Task TryGrantAsync_TwoConcurrentGrantsForSameReferral_OnlyOneCompletesFully()
    {
        var referrer = await CreateAccountAsync();
        var referee = await CreateAccountAsync();
        var referralId = Guid.NewGuid();

        var rewardA = LoyaltyReferralReward.Grant(referralId, referrer.UserId, referee.UserId, 200, 100, "referral.default", DateTime.UtcNow);
        var rewardB = LoyaltyReferralReward.Grant(referralId, referrer.UserId, referee.UserId, 200, 100, "referral.default", DateTime.UtcNow);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new LoyaltyReferralGrantRepository(context1).TryGrantAsync(
                rewardA, CreditRequest(referrer, referralId, 200), CreditRequest(referee, referralId, 100), DateTime.UtcNow, CancellationToken.None),
            new LoyaltyReferralGrantRepository(context2).TryGrantAsync(
                rewardB, CreditRequest(referrer, referralId, 200), CreditRequest(referee, referralId, 100), DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);

        await using var readContext = _fixture.CreateContext();
        var accountRepository = new LoyaltyAccountRepository(readContext);
        // Exactly one grant's worth of points landed — not zero, not double.
        Assert.Equal(200, (await accountRepository.GetByIdAsync(referrer.Id, CancellationToken.None))!.CurrentRewardPoints);
        Assert.Equal(100, (await accountRepository.GetByIdAsync(referee.Id, CancellationToken.None))!.CurrentRewardPoints);
    }
}
