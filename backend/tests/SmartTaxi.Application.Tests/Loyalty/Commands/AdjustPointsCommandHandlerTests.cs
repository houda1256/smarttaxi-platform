using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Commands.AdjustPoints;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.Loyalty.Commands;

public class AdjustPointsCommandHandlerTests
{
    private readonly FakeLoyaltyAccountRepository _accountRepository = new();
    private readonly FakeLoyaltyPointLedgerRepository _ledgerRepository;
    private readonly FakeLoyaltyTierThresholdRepository _tierThresholdRepository = new();
    private readonly AdjustPointsCommandHandler _handler;

    public AdjustPointsCommandHandlerTests()
    {
        _ledgerRepository = new FakeLoyaltyPointLedgerRepository(_accountRepository);
        _handler = new AdjustPointsCommandHandler(_accountRepository, _ledgerRepository, _tierThresholdRepository);
    }

    private async Task<LoyaltyAccount> CreateAccountAsync(int initialRewardPoints = 0)
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await _accountRepository.TryAddAsync(account, CancellationToken.None);

        if (initialRewardPoints > 0)
        {
            account.ApplyRewardPointsDelta(initialRewardPoints, DateTime.UtcNow);
        }

        return account;
    }

    [Fact]
    public async Task Handle_PositiveAdjustment_CreditsAccount()
    {
        var account = await CreateAccountAsync();
        var adminId = Guid.NewGuid();

        var result = await _handler.Handle(
            new AdjustPointsCommand(adminId, account.UserId, LoyaltyPointType.RewardPoints, 50, "Geste commercial"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_NegativeAdjustmentWithSufficientBalance_DebitsAccount()
    {
        var account = await CreateAccountAsync(initialRewardPoints: 100);

        var result = await _handler.Handle(
            new AdjustPointsCommand(Guid.NewGuid(), account.UserId, LoyaltyPointType.RewardPoints, -30, "Correction"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(70, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_NegativeAdjustmentExceedingBalance_ReturnsConflict()
    {
        var account = await CreateAccountAsync(initialRewardPoints: 10);

        var result = await _handler.Handle(
            new AdjustPointsCommand(Guid.NewGuid(), account.UserId, LoyaltyPointType.RewardPoints, -30, "Correction"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(10, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_ZeroAmount_ReturnsValidationError()
    {
        var account = await CreateAccountAsync();

        var result = await _handler.Handle(
            new AdjustPointsCommand(Guid.NewGuid(), account.UserId, LoyaltyPointType.RewardPoints, 0, "Correction"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_NoAccount_ReturnsNotFound()
    {
        var result = await _handler.Handle(
            new AdjustPointsCommand(Guid.NewGuid(), Guid.NewGuid(), LoyaltyPointType.RewardPoints, 10, "Correction"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
