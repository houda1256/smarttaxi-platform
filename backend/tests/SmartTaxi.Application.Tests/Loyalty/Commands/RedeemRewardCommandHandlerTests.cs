using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Commands.RedeemReward;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.Loyalty.Commands;

public class RedeemRewardCommandHandlerTests
{
    private readonly FakeLoyaltyRewardRepository _rewardRepository = new();
    private readonly FakeLoyaltyAccountRepository _accountRepository = new();
    private readonly FakeLoyaltyRedemptionTransactionRepository _redemptionTransactionRepository;
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly RedeemRewardCommandHandler _handler;

    public RedeemRewardCommandHandlerTests()
    {
        _redemptionTransactionRepository = new FakeLoyaltyRedemptionTransactionRepository(_accountRepository, _rewardRepository);
        _handler = new RedeemRewardCommandHandler(_rewardRepository, _accountRepository, _redemptionTransactionRepository, _notificationDispatcher);
    }

    private async Task<LoyaltyAccount> CreateAccountWithBalanceAsync(int rewardPoints, UserRole role = UserRole.Customer)
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), role, DateTime.UtcNow);
        await _accountRepository.TryAddAsync(account, CancellationToken.None);
        account.ApplyRewardPointsDelta(rewardPoints, DateTime.UtcNow);
        return account;
    }

    private async Task<LoyaltyReward> CreateRewardAsync(
        int cost = 100, LoyaltyRewardType type = LoyaltyRewardType.FreeService, IReadOnlyList<UserRole>? roles = null, int? usageLimit = null)
    {
        var reward = LoyaltyReward.Create("REWARD", "Reward", "desc", cost, type, roles ?? [], null, null, usageLimit, DateTime.UtcNow);
        await _rewardRepository.AddAsync(reward, CancellationToken.None);
        return reward;
    }

    [Fact]
    public async Task Handle_SufficientBalance_Succeeds()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(40, account.CurrentRewardPoints);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.TemplateKey == "loyalty.reward-redeemed");
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ReturnsConflictAndLeavesRewardUnconsumed()
    {
        var account = await CreateAccountWithBalanceAsync(10);
        var reward = await CreateRewardAsync(cost: 60);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(10, account.CurrentRewardPoints);
        Assert.Equal(0, reward.RedeemedCount);
    }

    [Fact]
    public async Task Handle_InactiveReward_ReturnsConflict()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60);
        reward.Deactivate(DateTime.UtcNow);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Theory]
    [InlineData(LoyaltyRewardType.RideDiscount)]
    [InlineData(LoyaltyRewardType.SubscriptionDiscount)]
    public async Task Handle_NonExecutableRewardType_ReturnsValidationError(LoyaltyRewardType type)
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60, type: type);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(100, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_RewardRestrictedToAnotherRole_ReturnsForbidden()
    {
        var account = await CreateAccountWithBalanceAsync(100, UserRole.Customer);
        var reward = await CreateRewardAsync(cost: 60, roles: [UserRole.Driver]);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_UsageLimitAlreadyReached_ReturnsConflictWithNoDebitAtAll()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60, usageLimit: 1);
        await _rewardRepository.TryIncrementRedeemedCountAsync(reward.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        // Module 7 audit fix #4: the debit and the usage-limit reservation are one atomic transaction,
        // so a lost usage-limit race never touches the balance at all — no compensating refund needed.
        Assert.Equal(100, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_NoAccount_ReturnsNotFound()
    {
        var reward = await CreateRewardAsync();

        var result = await _handler.Handle(new RedeemRewardCommand(Guid.NewGuid(), reward.Id, "key-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_MissingIdempotencyKey_ReturnsValidationError()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60);

        var result = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(100, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_SameIdempotencyKeyRetried_DoesNotDebitTwice()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 60);

        var first = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "same-key"), CancellationToken.None);
        var second = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "same-key"), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal(40, account.CurrentRewardPoints);
        Assert.Equal(1, reward.RedeemedCount);
    }

    [Fact]
    public async Task Handle_SameRewardWithTwoDifferentKeys_ProducesTwoDistinctRedemptions()
    {
        var account = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 30);

        var first = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-a"), CancellationToken.None);
        var second = await _handler.Handle(new RedeemRewardCommand(account.UserId, reward.Id, "key-b"), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(40, account.CurrentRewardPoints);
        Assert.Equal(2, reward.RedeemedCount);
    }

    [Fact]
    public async Task Handle_SameIdempotencyKeyAcrossDifferentUsers_DoesNotConflict()
    {
        var accountA = await CreateAccountWithBalanceAsync(100);
        var accountB = await CreateAccountWithBalanceAsync(100);
        var reward = await CreateRewardAsync(cost: 30);

        var resultA = await _handler.Handle(new RedeemRewardCommand(accountA.UserId, reward.Id, "shared-key"), CancellationToken.None);
        var resultB = await _handler.Handle(new RedeemRewardCommand(accountB.UserId, reward.Id, "shared-key"), CancellationToken.None);

        Assert.True(resultA.IsSuccess);
        Assert.True(resultB.IsSuccess);
        Assert.NotEqual(resultA.Value, resultB.Value);
        Assert.Equal(70, accountA.CurrentRewardPoints);
        Assert.Equal(70, accountB.CurrentRewardPoints);
    }
}
