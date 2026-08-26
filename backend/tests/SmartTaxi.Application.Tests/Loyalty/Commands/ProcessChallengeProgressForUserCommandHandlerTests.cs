using SmartTaxi.Application.Loyalty.Commands.ProcessChallengeProgressForUser;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.Loyalty.Commands;

public class ProcessChallengeProgressForUserCommandHandlerTests
{
    private readonly FakeLoyaltyChallengeRepository _challengeRepository = new();
    private readonly FakeLoyaltyChallengeProgressRepository _progressRepository = new();
    private readonly FakeLoyaltyAccountRepository _accountRepository = new();
    private readonly FakeLoyaltyPointLedgerRepository _ledgerRepository;
    private readonly FakeLoyaltyReferralRewardRepository _referralRewardRepository = new();
    private readonly FakeLoyaltyTierThresholdRepository _tierThresholdRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly ProcessChallengeProgressForUserCommandHandler _handler;

    public ProcessChallengeProgressForUserCommandHandlerTests()
    {
        _ledgerRepository = new FakeLoyaltyPointLedgerRepository(_accountRepository);
        _handler = new ProcessChallengeProgressForUserCommandHandler(
            _challengeRepository, _progressRepository, _ledgerRepository, _referralRewardRepository, _accountRepository,
            _tierThresholdRepository, _notificationDispatcher);
    }

    /// <summary>Zero points per entry deliberately — these rows exist only to be counted as RideCount progress; the challenge's own reward is what should be the only balance change these tests observe.</summary>
    private async Task SeedRideCountEarnEntriesAsync(Guid userId, Guid accountId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _ledgerRepository.TryCreditAsync(
                new LoyaltyLedgerAppendRequest(accountId, userId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 0, "Payment", Guid.NewGuid(), "trajet"),
                [], DateTime.UtcNow, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Handle_ReachingTarget_GrantsRewardExactlyOnce()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await _accountRepository.TryAddAsync(account, CancellationToken.None);
        var challenge = LoyaltyChallenge.Create("TEN_RIDES", "10 courses", LoyaltyChallengeCriteriaType.RideCount, 3, 50, null, null, null, DateTime.UtcNow);
        await _challengeRepository.AddAsync(challenge, CancellationToken.None);
        await SeedRideCountEarnEntriesAsync(account.UserId, account.Id, 3);

        var result = await _handler.Handle(new ProcessChallengeProgressForUserCommand(account.UserId, UserRole.Customer), CancellationToken.None);

        Assert.Equal(1, result.Value);
        Assert.Equal(50, account.CurrentRewardPoints);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.TemplateKey == "loyalty.challenge-completed");
    }

    [Fact]
    public async Task Handle_CalledTwiceAfterCompletion_NeverRewardsTwice()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await _accountRepository.TryAddAsync(account, CancellationToken.None);
        var challenge = LoyaltyChallenge.Create("TEN_RIDES", "10 courses", LoyaltyChallengeCriteriaType.RideCount, 3, 50, null, null, null, DateTime.UtcNow);
        await _challengeRepository.AddAsync(challenge, CancellationToken.None);
        await SeedRideCountEarnEntriesAsync(account.UserId, account.Id, 5);

        await _handler.Handle(new ProcessChallengeProgressForUserCommand(account.UserId, UserRole.Customer), CancellationToken.None);
        var second = await _handler.Handle(new ProcessChallengeProgressForUserCommand(account.UserId, UserRole.Customer), CancellationToken.None);

        Assert.Equal(0, second.Value);
        Assert.Equal(50, account.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_BelowTarget_DoesNotReward()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow);
        await _accountRepository.TryAddAsync(account, CancellationToken.None);
        var challenge = LoyaltyChallenge.Create("TEN_RIDES", "10 courses", LoyaltyChallengeCriteriaType.RideCount, 10, 50, null, null, null, DateTime.UtcNow);
        await _challengeRepository.AddAsync(challenge, CancellationToken.None);
        await SeedRideCountEarnEntriesAsync(account.UserId, account.Id, 3);

        var result = await _handler.Handle(new ProcessChallengeProgressForUserCommand(account.UserId, UserRole.Customer), CancellationToken.None);

        Assert.Equal(0, result.Value);
        Assert.Equal(0, account.CurrentRewardPoints);
    }
}
