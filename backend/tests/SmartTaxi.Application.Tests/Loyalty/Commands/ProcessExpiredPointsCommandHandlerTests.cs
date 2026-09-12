using SmartTaxi.Application.Loyalty.Commands.ProcessExpiredPoints;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.Loyalty.Commands;

public class ProcessExpiredPointsCommandHandlerTests
{
    private readonly FakeLoyaltyAccountRepository _accountRepository = new();
    private readonly FakeLoyaltyPointLedgerRepository _ledgerRepository;
    private readonly ProcessExpiredPointsCommandHandler _handler;

    public ProcessExpiredPointsCommandHandlerTests()
    {
        _ledgerRepository = new FakeLoyaltyPointLedgerRepository(_accountRepository);
        _handler = new ProcessExpiredPointsCommandHandler(_ledgerRepository);
    }

    private async Task<LoyaltyAccount> CreateAccountAsync()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await _accountRepository.TryAddAsync(account, CancellationToken.None);
        return account;
    }

    [Fact]
    public async Task Handle_DueEarnEntryWithFullBalanceRemaining_ExpiresFullAmount()
    {
        var account = await CreateAccountAsync();
        var pastExpiry = DateTime.UtcNow.AddDays(-1);

        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "trajet", ExpirationAtUtc: pastExpiry),
            [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None);

        var result = await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Value);
        Assert.Equal(0, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_DueEarnEntryPartiallyAlreadySpent_ExpiresOnlyRemainingBalance()
    {
        var account = await CreateAccountAsync();
        var pastExpiry = DateTime.UtcNow.AddDays(-1);
        var earnRequest = new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "trajet", ExpirationAtUtc: pastExpiry);
        await _ledgerRepository.TryCreditAsync(earnRequest, [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None);

        // Spend 70 of the 100 via an unrelated redemption before the sweep runs.
        await _ledgerRepository.TryDebitAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 70, "Redemption", Guid.NewGuid(), "échange"),
            DateTime.UtcNow, CancellationToken.None);

        await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);

        Assert.Equal(0, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_CalledTwice_NeverExpiresSameBatchTwice()
    {
        var account = await CreateAccountAsync();
        var pastExpiry = DateTime.UtcNow.AddDays(-1);
        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "trajet", ExpirationAtUtc: pastExpiry),
            [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None);

        var first = await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);
        var second = await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);

        Assert.Equal(1, first.Value);
        Assert.Equal(0, second.Value);
    }

    [Fact]
    public async Task Handle_NotYetDue_IsNotExpired()
    {
        var account = await CreateAccountAsync();
        var futureExpiry = DateTime.UtcNow.AddMonths(1);
        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "trajet", ExpirationAtUtc: futureExpiry),
            [], DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Value);
        Assert.Equal(100, account.CurrentRewardPoints);
    }

    /// <summary>
    /// Module 7 audit fix #2 scenario A: Earn A=100 expires today, Earn B=100
    /// expires next year, and 100 is redeemed before A's expiration. FEFO
    /// consumption must draw the redemption from A (soonest-expiring) first,
    /// so A's own expiration removes nothing and B's untouched 100 survives.
    /// </summary>
    [Fact]
    public async Task Handle_MultiLotRedeemBeforeEarliestExpiration_ExpiresOnlyTheConsumedLotNotTheUntouchedOne()
    {
        var account = await CreateAccountAsync();
        var lotAExpiry = DateTime.UtcNow.AddDays(-1);
        var lotBExpiry = DateTime.UtcNow.AddYears(1);

        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "lot A", ExpirationAtUtc: lotAExpiry),
            [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None);
        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "lot B", ExpirationAtUtc: lotBExpiry),
            [], DateTime.UtcNow, CancellationToken.None);

        await _ledgerRepository.TryDebitAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 100, "Redemption", Guid.NewGuid(), "échange"),
            DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);

        // Lot A is fully consumed (Remaining=0) so its expiration removes 0; only lot B's untouched
        // 100 remains on the account, and it is not due for expiration for another year.
        Assert.Equal(1, result.Value);
        Assert.Equal(100, account.CurrentRewardPoints);
    }

    /// <summary>Module 7 audit fix #2 scenario B: redeeming only half of lot A leaves exactly 50 in lot A (and lot B untouched), so A's expiration removes exactly 50.</summary>
    [Fact]
    public async Task Handle_MultiLotPartialRedeemOfEarliestLot_ExpiresExactlyItsRemainingAmount()
    {
        var account = await CreateAccountAsync();
        var lotAExpiry = DateTime.UtcNow.AddDays(-1);
        var lotBExpiry = DateTime.UtcNow.AddYears(1);

        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "lot A", ExpirationAtUtc: lotAExpiry),
            [], DateTime.UtcNow.AddMonths(-13), CancellationToken.None);
        await _ledgerRepository.TryCreditAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 100, "Payment", Guid.NewGuid(), "lot B", ExpirationAtUtc: lotBExpiry),
            [], DateTime.UtcNow, CancellationToken.None);

        await _ledgerRepository.TryDebitAsync(
            new LoyaltyLedgerAppendRequest(account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, 50, "Redemption", Guid.NewGuid(), "échange"),
            DateTime.UtcNow, CancellationToken.None);

        // Balance before expiration: 200 - 50 = 150 (lot A has 50 left, lot B untouched at 100).
        Assert.Equal(150, account.CurrentRewardPoints);

        await _handler.Handle(new ProcessExpiredPointsCommand(), CancellationToken.None);

        // Only lot A's remaining 50 expires; lot B's 100 survives.
        Assert.Equal(100, account.CurrentRewardPoints);
    }
}
