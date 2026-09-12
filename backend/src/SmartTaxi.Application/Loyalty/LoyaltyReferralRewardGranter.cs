using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Loyalty;

/// <summary>
/// The shared logic behind granting a referral reward — used both by the
/// real-time path (a payer who is themselves a referee, evaluated right after
/// their payment confirms) and the bulk catch-up sweep
/// (ProcessReferralRewardsCommand). Identity determines the Referral
/// relationship and its RewardEligible status; this class only ever reads
/// that status, it never mutates the Referral itself. The actual grant (reward
/// row + both credits) is delegated to ILoyaltyReferralGrantRepository as one
/// all-or-nothing transaction (Module 7 audit fix #1) — this class only
/// resolves eligibility/accounts and dispatches the resulting notifications,
/// which are best-effort and safe to run after the transaction commits.
/// </summary>
public sealed class LoyaltyReferralRewardGranter
{
    private readonly ILoyaltyReferralRewardRepository _referralRewardRepository;
    private readonly ILoyaltyReferralGrantRepository _grantRepository;
    private readonly ILoyaltyAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILoyaltyReferralRewardPolicy _policy;
    private readonly INotificationDispatcher _notificationDispatcher;

    public LoyaltyReferralRewardGranter(
        ILoyaltyReferralRewardRepository referralRewardRepository, ILoyaltyReferralGrantRepository grantRepository,
        ILoyaltyAccountRepository accountRepository, IUserRepository userRepository, ILoyaltyReferralRewardPolicy policy,
        INotificationDispatcher notificationDispatcher)
    {
        _referralRewardRepository = referralRewardRepository;
        _grantRepository = grantRepository;
        _accountRepository = accountRepository;
        _userRepository = userRepository;
        _policy = policy;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<bool> TryGrantIfEligibleAsync(Referral referral, CancellationToken cancellationToken)
    {
        if (referral.Status != ReferralStatus.RewardEligible)
        {
            return false;
        }

        if (await _referralRewardRepository.GetByReferralIdAsync(referral.Id, cancellationToken) is not null)
        {
            return false;
        }

        if (_policy.ReferrerRewardPoints <= 0 && _policy.RefereeRewardPoints <= 0)
        {
            return false;
        }

        var referrerAccount = await EnsureAccountAsync(referral.ReferrerUserId, cancellationToken);
        var refereeAccount = await EnsureAccountAsync(referral.RefereeUserId, cancellationToken);

        if (referrerAccount is null || refereeAccount is null)
        {
            // Neither party is an eligible actor (Customer/Driver) — nothing Loyalty can reward.
            return false;
        }

        var utcNow = DateTime.UtcNow;
        var reason = $"Récompense de parrainage ({_policy.RuleCode})";

        var reward = LoyaltyReferralReward.Grant(
            referral.Id, referral.ReferrerUserId, referral.RefereeUserId, _policy.ReferrerRewardPoints, _policy.RefereeRewardPoints,
            _policy.RuleCode, utcNow);

        var referrerRequest = _policy.ReferrerRewardPoints > 0
            ? new LoyaltyLedgerAppendRequest(
                referrerAccount.Id, referrerAccount.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ReferralReward,
                _policy.ReferrerRewardPoints, "Referral", referral.Id, reason, ReferralId: referral.Id)
            : null;

        var refereeRequest = _policy.RefereeRewardPoints > 0
            ? new LoyaltyLedgerAppendRequest(
                refereeAccount.Id, refereeAccount.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ReferralReward,
                _policy.RefereeRewardPoints, "Referral", referral.Id, reason, ReferralId: referral.Id)
            : null;

        // All-or-nothing: the reward row and both credits either all land in this one
        // transaction or none of them do — see ILoyaltyReferralGrantRepository.
        if (!await _grantRepository.TryGrantAsync(reward, referrerRequest, refereeRequest, utcNow, cancellationToken))
        {
            return false;
        }

        if (referrerRequest is not null)
        {
            await NotifyAsync(referrerAccount.UserId, referral.Id, _policy.ReferrerRewardPoints, cancellationToken);
        }

        if (refereeRequest is not null)
        {
            await NotifyAsync(refereeAccount.UserId, referral.Id, _policy.RefereeRewardPoints, cancellationToken);
        }

        return true;
    }

    private Task NotifyAsync(Guid recipientUserId, Guid referralId, int points, CancellationToken cancellationToken) =>
        _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                recipientUserId, NotificationCategory.Loyalty, "loyalty.referral-reward-granted",
                new Dictionary<string, string> { ["Points"] = points.ToString() }, IsMandatory: false, SourceType: "Referral",
                SourceId: referralId),
            cancellationToken);

    private async Task<LoyaltyAccount?> EnsureAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var eligibleRole = user is null ? null : ResolveEligibleRole(user.Roles);

        if (eligibleRole is null)
        {
            return null;
        }

        var account = LoyaltyAccount.Open(userId, eligibleRole.Value, DateTime.UtcNow);

        if (await _accountRepository.TryAddAsync(account, cancellationToken))
        {
            return account;
        }

        // Lost a race with another concurrent account-open for the same user.
        return await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    /// <summary>A user may hold several roles (multi-role Identity) — Loyalty picks whichever eligible one (Driver over Customer) it recognizes, since only those two currently earn/hold a LoyaltyAccount.</summary>
    private static UserRole? ResolveEligibleRole(IReadOnlyCollection<UserRole> roles)
    {
        if (roles.Contains(UserRole.Driver))
        {
            return UserRole.Driver;
        }

        return roles.Contains(UserRole.Customer) ? UserRole.Customer : null;
    }
}
