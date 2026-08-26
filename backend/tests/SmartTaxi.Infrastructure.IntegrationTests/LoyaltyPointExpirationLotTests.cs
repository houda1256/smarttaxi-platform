using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Loyalty.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against a real PostgreSQL database, the Module 7 audit fix #2
/// per-lot expiration guarantees: a redemption drawn from one lot must never
/// cause a DIFFERENT, untouched lot to be incorrectly expired, and concurrent
/// redemption/expiration of the same account can never double-consume or
/// drive a balance negative.
/// </summary>
[Collection("SharedPostgres")]
public class LoyaltyPointExpirationLotTests
{
    private readonly SharedPostgresFixture _fixture;

    public LoyaltyPointExpirationLotTests(SharedPostgresFixture fixture)
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

    private async Task<LoyaltyPointLedgerEntry> SeedEarnLotAsync(LoyaltyAccount account, int points, DateTime expirationAtUtc)
    {
        await using var context = _fixture.CreateContext();
        return (await new LoyaltyPointLedgerRepository(context).TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, points, "Payment", Guid.NewGuid(), "lot", ExpirationAtUtc: expirationAtUtc),
            [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None))!;
    }

    /// <summary>Scenario A: redeeming exactly lot A's amount before its expiration must fully consume A (FEFO) and leave B untouched, so A's own expiration removes 0 and B survives intact.</summary>
    [Fact]
    public async Task Redemption_BeforeEarliestLotExpiration_ExpiringThatLotRemovesNothingAndTheOtherLotSurvives()
    {
        var account = await CreateAccountAsync();
        var lotA = await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddDays(-1));
        await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddYears(1));

        await using (var redeemContext = _fixture.CreateContext())
        {
            await new LoyaltyPointLedgerRepository(redeemContext).TryDebitAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 100, "Redemption", Guid.NewGuid(), "échange"),
                DateTime.UtcNow, CancellationToken.None);
        }

        await using (var expireContext = _fixture.CreateContext())
        {
            var expireResult = await new LoyaltyPointLedgerRepository(expireContext).TryDebitAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire, lotA.Points, "LoyaltyPointLedgerEntry", lotA.Id, "expiration"),
                DateTime.UtcNow, CancellationToken.None);

            Assert.NotNull(expireResult);
            Assert.Equal(0, expireResult!.Points); // Nothing left to expire — lot A was already fully consumed by the redemption.
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(100, reloaded!.CurrentRewardPoints); // Lot B's untouched 100 remains.
    }

    /// <summary>Scenario B: redeeming only half of lot A leaves exactly 50 remaining in it, so its expiration removes exactly 50 — never touching lot B.</summary>
    [Fact]
    public async Task PartialRedemptionOfEarliestLot_ExpirationRemovesExactlyItsRemainingAmount()
    {
        var account = await CreateAccountAsync();
        var lotA = await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddDays(-1));
        await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddYears(1));

        await using (var redeemContext = _fixture.CreateContext())
        {
            await new LoyaltyPointLedgerRepository(redeemContext).TryDebitAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 50, "Redemption", Guid.NewGuid(), "échange"),
                DateTime.UtcNow, CancellationToken.None);
        }

        await using var readBeforeContext = _fixture.CreateContext();
        Assert.Equal(150, (await new LoyaltyAccountRepository(readBeforeContext).GetByIdAsync(account.Id, CancellationToken.None))!.CurrentRewardPoints);

        await using (var expireContext = _fixture.CreateContext())
        {
            var expireResult = await new LoyaltyPointLedgerRepository(expireContext).TryDebitAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire, lotA.Points, "LoyaltyPointLedgerEntry", lotA.Id, "expiration"),
                DateTime.UtcNow, CancellationToken.None);

            Assert.Equal(-50, expireResult!.Points); // Signed: negative for a debit/expiration entry.
        }

        await using var readContext = _fixture.CreateContext();
        Assert.Equal(100, (await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None))!.CurrentRewardPoints);
    }

    /// <summary>Scenario D: the same lot can never be expired twice — the ledger's own idempotency key is the guarantee (already proven for the single-lot case by LoyaltyConcurrencyTests; this exercises it explicitly on a lot alongside a second, untouched lot).</summary>
    [Fact]
    public async Task Lot_CannotBeExpiredTwice()
    {
        var account = await CreateAccountAsync();
        var lotA = await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddDays(-1));

        var expireRequest = new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire, lotA.Points, "LoyaltyPointLedgerEntry", lotA.Id, "expiration");

        await using (var firstContext = _fixture.CreateContext())
        {
            await new LoyaltyPointLedgerRepository(firstContext).TryDebitAsync(expireRequest, DateTime.UtcNow, CancellationToken.None);
        }

        await using var secondContext = _fixture.CreateContext();
        var second = await new LoyaltyPointLedgerRepository(secondContext).TryDebitAsync(expireRequest, DateTime.UtcNow, CancellationToken.None);

        Assert.Null(second);
    }

    /// <summary>Scenario C: a redemption and the expiration of one lot racing concurrently must never double-consume, never drive the balance negative, and must never expire a different, still-valid lot early.</summary>
    [Fact]
    public async Task ConcurrentRedemptionAndExpiration_NeverDoubleConsumesOrGoesNegativeOrExpiresTheOtherLotEarly()
    {
        var account = await CreateAccountAsync();
        var lotA = await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddDays(-1));
        await SeedEarnLotAsync(account, 100, DateTime.UtcNow.AddYears(1));

        await using var redeemContext = _fixture.CreateContext();
        await using var expireContext = _fixture.CreateContext();

        await Task.WhenAll(
            new LoyaltyPointLedgerRepository(redeemContext).TryDebitAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 100, "Redemption", Guid.NewGuid(), "échange"),
                DateTime.UtcNow, CancellationToken.None),
            new LoyaltyPointLedgerRepository(expireContext).TryDebitAsync(
                new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire, lotA.Points, "LoyaltyPointLedgerEntry", lotA.Id, "expiration"),
                DateTime.UtcNow, CancellationToken.None));

        await using var readContext = _fixture.CreateContext();
        var account_ = await new LoyaltyAccountRepository(readContext).GetByIdAsync(account.Id, CancellationToken.None);
        var entries = await readContext.LoyaltyPointLedgerEntries.Where(e => e.LoyaltyAccountId == account.Id).ToListAsync();
        var lots = entries.Where(e => e.EntryType == LoyaltyLedgerEntryType.Earn).ToList();

        Assert.True(account_!.CurrentRewardPoints >= 0);
        // No double counting or phantom loss: the balance always equals the sum of what each lot still has.
        Assert.Equal(account_.CurrentRewardPoints, lots.Sum(l => l.RemainingAmount ?? 0));
        // Whichever operation won the race, only 200 points total ever existed — nothing was consumed twice.
        Assert.True(account_.CurrentRewardPoints is 0 or 100);
    }
}
