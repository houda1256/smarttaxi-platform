using SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;
using SmartTaxi.Application.Loyalty;
using SmartTaxi.Application.Loyalty.Commands.ProcessChallengeProgressForUser;
using SmartTaxi.Application.Loyalty.Commands.ProcessPaymentLoyaltyAward;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.Loyalty.Commands;

public class ProcessPaymentLoyaltyAwardCommandHandlerTests
{
    private readonly FakeLoyaltyAccountRepository _accountRepository = new();
    private readonly FakeLoyaltyEarningRuleRepository _earningRuleRepository = new();
    private readonly FakeLoyaltyPointLedgerRepository _ledgerRepository;
    private readonly FakeLoyaltyTierThresholdRepository _tierThresholdRepository = new();
    private readonly FakeLoyaltyPointExpirationPolicy _expirationPolicy = new();
    private readonly FakeSubscriptionEntitlementService _entitlementService = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly FakeReferralRepository _referralRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeLoyaltyReferralRewardRepository _referralRewardRepository = new();
    private readonly FakeLoyaltyReferralRewardPolicy _referralRewardPolicy = new();
    private readonly FakeLoyaltyChallengeRepository _challengeRepository = new();
    private readonly FakeLoyaltyChallengeProgressRepository _challengeProgressRepository = new();

    private readonly ProcessPaymentLoyaltyAwardCommandHandler _handler;

    public ProcessPaymentLoyaltyAwardCommandHandlerTests()
    {
        _ledgerRepository = new FakeLoyaltyPointLedgerRepository(_accountRepository);

        var evaluateReferralActivationHandler = new EvaluateReferralActivationCommandHandler(
            _referralRepository, _userRepository, new FakeReferralActivationPolicy());

        var grantRepository = new FakeLoyaltyReferralGrantRepository(_accountRepository, _referralRewardRepository);

        var referralRewardGranter = new LoyaltyReferralRewardGranter(
            _referralRewardRepository, grantRepository, _accountRepository, _userRepository, _referralRewardPolicy, _notificationDispatcher);

        var challengeProgressHandler = new ProcessChallengeProgressForUserCommandHandler(
            _challengeRepository, _challengeProgressRepository, _ledgerRepository, _referralRewardRepository, _accountRepository,
            _tierThresholdRepository, _notificationDispatcher);

        _handler = new ProcessPaymentLoyaltyAwardCommandHandler(
            _accountRepository, _earningRuleRepository, _ledgerRepository, _tierThresholdRepository, _expirationPolicy,
            _entitlementService, _notificationDispatcher, _referralRepository, evaluateReferralActivationHandler, referralRewardGranter,
            challengeProgressHandler);
    }

    private async Task SeedRuleAsync(decimal rewardRate = 1m, decimal statusRate = 1m, bool multiplierAllowed = true, UserRole role = UserRole.Customer)
    {
        var rule = Domain.Loyalty.Entities.LoyaltyEarningRule.Create(
            "RIDE_PAID", role, "Payment", rewardRate, statusRate, null, null, multiplierAllowed, null, null, DateTime.UtcNow);
        await _earningRuleRepository.AddAsync(rule, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithActiveRule_CreditsAccountAndNotifies()
    {
        await SeedRuleAsync(rewardRate: 1m, statusRate: 1m);
        var payerId = Guid.NewGuid();

        var result = await _handler.Handle(
            new ProcessPaymentLoyaltyAwardCommand(payerId, UserRole.Customer, Guid.NewGuid(), 20m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var account = await _accountRepository.GetByUserIdAsync(payerId, CancellationToken.None);
        Assert.Equal(20, account!.CurrentRewardPoints);
        Assert.Equal(20, account.CurrentStatusPoints);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.TemplateKey == "loyalty.points-earned");
    }

    [Fact]
    public async Task Handle_CalledTwiceForSamePayment_IsIdempotent()
    {
        await SeedRuleAsync(rewardRate: 1m, statusRate: 0m);
        var payerId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var command = new ProcessPaymentLoyaltyAwardCommand(payerId, UserRole.Customer, paymentId, 20m);

        await _handler.Handle(command, CancellationToken.None);
        await _handler.Handle(command, CancellationToken.None);

        var account = await _accountRepository.GetByUserIdAsync(payerId, CancellationToken.None);
        Assert.Equal(20, account!.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_WithoutMatchingRule_IsNoOpSuccess()
    {
        var payerId = Guid.NewGuid();

        var result = await _handler.Handle(
            new ProcessPaymentLoyaltyAwardCommand(payerId, UserRole.Customer, Guid.NewGuid(), 20m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await _accountRepository.GetByUserIdAsync(payerId, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithSubscriptionMultiplier_IncreasesRewardPoints()
    {
        await SeedRuleAsync(rewardRate: 1m, statusRate: 0m, multiplierAllowed: true);
        var payerId = Guid.NewGuid();
        _entitlementService.Entitlement = new SubscriptionEntitlement(
            Guid.NewGuid(), Guid.NewGuid(), [], new Dictionary<string, int> { ["LoyaltyPointsMultiplierPercent"] = 150 }, DateTime.UtcNow.AddMonths(1));

        await _handler.Handle(new ProcessPaymentLoyaltyAwardCommand(payerId, UserRole.Customer, Guid.NewGuid(), 20m), CancellationToken.None);

        var account = await _accountRepository.GetByUserIdAsync(payerId, CancellationToken.None);
        Assert.Equal(30, account!.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_WithNoSubscription_FallsBackToDefaultMultiplier()
    {
        await SeedRuleAsync(rewardRate: 1m, statusRate: 0m, multiplierAllowed: true);
        var payerId = Guid.NewGuid();
        _entitlementService.Entitlement = null;

        await _handler.Handle(new ProcessPaymentLoyaltyAwardCommand(payerId, UserRole.Customer, Guid.NewGuid(), 20m), CancellationToken.None);

        var account = await _accountRepository.GetByUserIdAsync(payerId, CancellationToken.None);
        Assert.Equal(20, account!.CurrentRewardPoints);
    }

    [Fact]
    public async Task Handle_WithIneligibleActorRole_SkipsEarningButStillNoOpSuccess()
    {
        await SeedRuleAsync();

        var result = await _handler.Handle(
            new ProcessPaymentLoyaltyAwardCommand(Guid.NewGuid(), UserRole.Admin, Guid.NewGuid(), 20m), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenPayerIsRefereeWithSatisfiedConditions_GrantsReferralReward()
    {
        var referrer = User.Create(Email.Create($"referrer-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        var referee = User.Create(Email.Create($"referee-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        referee.VerifyEmail(DateTime.UtcNow.AddDays(-10));
        await _userRepository.AddAsync(referrer, CancellationToken.None);
        await _userRepository.AddAsync(referee, CancellationToken.None);

        var referral = new Referral(referrer.Id, referee.Id, "CODE123", DateTime.UtcNow.AddDays(-10));
        await _referralRepository.AddAsync(referral, CancellationToken.None);

        await _handler.Handle(new ProcessPaymentLoyaltyAwardCommand(referee.Id, UserRole.Customer, Guid.NewGuid(), 20m), CancellationToken.None);

        var reward = await _referralRewardRepository.GetByReferralIdAsync(referral.Id, CancellationToken.None);
        Assert.NotNull(reward);
        var referrerAccount = await _accountRepository.GetByUserIdAsync(referrer.Id, CancellationToken.None);
        Assert.Equal(_referralRewardPolicy.ReferrerRewardPoints, referrerAccount!.CurrentRewardPoints);
    }
}
