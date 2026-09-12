using SmartTaxi.Application.Loyalty;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Loyalty;

public class LoyaltyReferralRewardGranterTests
{
    private readonly FakeLoyaltyReferralRewardRepository _referralRewardRepository = new();
    private readonly FakeLoyaltyAccountRepository _accountRepository = new();
    private readonly FakeLoyaltyReferralGrantRepository _grantRepository;
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeLoyaltyReferralRewardPolicy _policy = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly LoyaltyReferralRewardGranter _granter;

    public LoyaltyReferralRewardGranterTests()
    {
        _grantRepository = new FakeLoyaltyReferralGrantRepository(_accountRepository, _referralRewardRepository);
        _granter = new LoyaltyReferralRewardGranter(
            _referralRewardRepository, _grantRepository, _accountRepository, _userRepository, _policy, _notificationDispatcher);
    }

    private async Task<(User Referrer, User Referee, Referral Referral)> CreateRewardEligibleReferralAsync()
    {
        var referrer = User.Create(Email.Create($"referrer-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Driver, DateTime.UtcNow);
        var referee = User.Create(Email.Create($"referee-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(referrer, CancellationToken.None);
        await _userRepository.AddAsync(referee, CancellationToken.None);

        var referral = new Referral(referrer.Id, referee.Id, "CODE1", DateTime.UtcNow.AddDays(-5));
        typeof(Referral).GetProperty(nameof(Referral.Status))!.SetValue(referral, Domain.Identity.Referrals.Enums.ReferralStatus.RewardEligible);

        return (referrer, referee, referral);
    }

    [Fact]
    public async Task TryGrantIfEligibleAsync_RewardsBothParties()
    {
        var (referrer, referee, referral) = await CreateRewardEligibleReferralAsync();

        var granted = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);

        Assert.True(granted);
        var referrerAccount = await _accountRepository.GetByUserIdAsync(referrer.Id, CancellationToken.None);
        var refereeAccount = await _accountRepository.GetByUserIdAsync(referee.Id, CancellationToken.None);
        Assert.Equal(_policy.ReferrerRewardPoints, referrerAccount!.CurrentRewardPoints);
        Assert.Equal(_policy.RefereeRewardPoints, refereeAccount!.CurrentRewardPoints);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.RecipientUserId == referrer.Id);
        Assert.Contains(_notificationDispatcher.DispatchedRequests, r => r.RecipientUserId == referee.Id);
    }

    [Fact]
    public async Task TryGrantIfEligibleAsync_CalledTwice_OnlyRewardsOnce()
    {
        var (_, _, referral) = await CreateRewardEligibleReferralAsync();

        var first = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);
        var second = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task TryGrantIfEligibleAsync_NotYetRewardEligible_ReturnsFalse()
    {
        var referrer = User.Create(Email.Create($"referrer-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Driver, DateTime.UtcNow);
        var referee = User.Create(Email.Create($"referee-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(referrer, CancellationToken.None);
        await _userRepository.AddAsync(referee, CancellationToken.None);
        var referral = new Referral(referrer.Id, referee.Id, "CODE1", DateTime.UtcNow);

        var granted = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);

        Assert.False(granted);
        Assert.Null(await _accountRepository.GetByUserIdAsync(referrer.Id, CancellationToken.None));
    }

    /// <summary>Module 7 audit fix #1: a failure anywhere in the grant transaction must leave neither party credited and no reward row behind.</summary>
    [Fact]
    public async Task TryGrantIfEligibleAsync_TransactionFails_NeitherPartyIsCreditedAndNoRewardRowRemains()
    {
        var (referrer, referee, referral) = await CreateRewardEligibleReferralAsync();
        _grantRepository.FailNextAttempt = true;

        var granted = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);

        Assert.False(granted);
        Assert.Null(await _referralRewardRepository.GetByReferralIdAsync(referral.Id, CancellationToken.None));
        var referrerAccount = await _accountRepository.GetByUserIdAsync(referrer.Id, CancellationToken.None);
        var refereeAccount = await _accountRepository.GetByUserIdAsync(referee.Id, CancellationToken.None);
        Assert.Equal(0, referrerAccount!.CurrentRewardPoints);
        Assert.Equal(0, refereeAccount!.CurrentRewardPoints);
    }

    /// <summary>Module 7 audit fix #1: after a failed attempt, the referral must remain retryable and the retry must succeed normally.</summary>
    [Fact]
    public async Task TryGrantIfEligibleAsync_RetryAfterFailure_SucceedsNormally()
    {
        var (referrer, referee, referral) = await CreateRewardEligibleReferralAsync();
        _grantRepository.FailNextAttempt = true;

        var firstAttempt = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);
        var retry = await _granter.TryGrantIfEligibleAsync(referral, CancellationToken.None);

        Assert.False(firstAttempt);
        Assert.True(retry);
        var referrerAccount = await _accountRepository.GetByUserIdAsync(referrer.Id, CancellationToken.None);
        var refereeAccount = await _accountRepository.GetByUserIdAsync(referee.Id, CancellationToken.None);
        Assert.Equal(_policy.ReferrerRewardPoints, referrerAccount!.CurrentRewardPoints);
        Assert.Equal(_policy.RefereeRewardPoints, refereeAccount!.CurrentRewardPoints);
    }
}
