using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Commands.ProcessChallengeProgressForUser;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Referrals.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessPaymentLoyaltyAward;

/// <summary>
/// Confirmed Payment -> eligible actor -> active earning rule -> subscription
/// multiplier -> RewardPoints + StatusPoints -> atomic ledger append -> tier
/// recalculation -> challenge progress -> notification, then (independently)
/// evaluates the payer's own referral, reusing Identity's existing
/// EvaluateReferralActivationCommandHandler exactly as-is — Identity's referral
/// business rules are never changed, only invoked. A missing rule, an
/// ineligible role, or no referral relationship are all legitimate no-ops
/// (Result.Success), never errors.
/// </summary>
public sealed class ProcessPaymentLoyaltyAwardCommandHandler : ICommandHandler<ProcessPaymentLoyaltyAwardCommand, Result>
{
    private const string MultiplierLimitKey = "LoyaltyPointsMultiplierPercent";
    private const string PaymentSourceType = "Payment";
    private const int DefaultMultiplierPercent = 100;
    private const int MaxMultiplierPercent = 1000;

    private readonly ILoyaltyAccountRepository _accountRepository;
    private readonly ILoyaltyEarningRuleRepository _earningRuleRepository;
    private readonly ILoyaltyPointLedgerRepository _ledgerRepository;
    private readonly ILoyaltyTierThresholdRepository _tierThresholdRepository;
    private readonly ILoyaltyPointExpirationPolicy _expirationPolicy;
    private readonly ISubscriptionEntitlementService _entitlementService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IReferralRepository _referralRepository;
    private readonly EvaluateReferralActivationCommandHandler _evaluateReferralActivationHandler;
    private readonly LoyaltyReferralRewardGranter _referralRewardGranter;
    private readonly ProcessChallengeProgressForUserCommandHandler _challengeProgressHandler;

    public ProcessPaymentLoyaltyAwardCommandHandler(
        ILoyaltyAccountRepository accountRepository, ILoyaltyEarningRuleRepository earningRuleRepository,
        ILoyaltyPointLedgerRepository ledgerRepository, ILoyaltyTierThresholdRepository tierThresholdRepository,
        ILoyaltyPointExpirationPolicy expirationPolicy, ISubscriptionEntitlementService entitlementService,
        INotificationDispatcher notificationDispatcher, IReferralRepository referralRepository,
        EvaluateReferralActivationCommandHandler evaluateReferralActivationHandler, LoyaltyReferralRewardGranter referralRewardGranter,
        ProcessChallengeProgressForUserCommandHandler challengeProgressHandler)
    {
        _accountRepository = accountRepository;
        _earningRuleRepository = earningRuleRepository;
        _ledgerRepository = ledgerRepository;
        _tierThresholdRepository = tierThresholdRepository;
        _expirationPolicy = expirationPolicy;
        _entitlementService = entitlementService;
        _notificationDispatcher = notificationDispatcher;
        _referralRepository = referralRepository;
        _evaluateReferralActivationHandler = evaluateReferralActivationHandler;
        _referralRewardGranter = referralRewardGranter;
        _challengeProgressHandler = challengeProgressHandler;
    }

    public async Task<Result> Handle(ProcessPaymentLoyaltyAwardCommand command, CancellationToken cancellationToken)
    {
        if (command.PayerRole is UserRole.Customer or UserRole.Driver)
        {
            await AwardEarningAsync(command, cancellationToken);
        }

        await TryEvaluateReferralAsync(command.PayerUserId, cancellationToken);

        return Result.Success();
    }

    private async Task AwardEarningAsync(ProcessPaymentLoyaltyAwardCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        // Rule lookup happens before account provisioning — lazily opening a LoyaltyAccount only
        // when there is actually an effective rule to earn from avoids creating empty accounts for
        // every payment while Loyalty is unconfigured for that role/source.
        var rule = await _earningRuleRepository.GetEffectiveAsync(command.PayerRole, PaymentSourceType, utcNow, cancellationToken);

        if (rule is null)
        {
            return;
        }

        var account = await _accountRepository.GetByUserIdAsync(command.PayerUserId, cancellationToken);

        if (account is null)
        {
            var opened = LoyaltyAccount.Open(command.PayerUserId, command.PayerRole, utcNow);
            account = await _accountRepository.TryAddAsync(opened, cancellationToken)
                ? opened
                : await _accountRepository.GetByUserIdAsync(command.PayerUserId, cancellationToken);
        }

        if (account is null)
        {
            return;
        }

        var multiplierPercent = await ResolveSubscriptionMultiplierAsync(
            command.PayerUserId, command.PayerRole, rule.SubscriptionMultiplierAllowed, cancellationToken);
        var (rewardPoints, statusPoints) = rule.CalculatePoints(command.Amount, multiplierPercent);
        var thresholds = await _tierThresholdRepository.GetAllAsync(cancellationToken);

        if (rewardPoints > 0)
        {
            var expirationAtUtc = _expirationPolicy.ExpirationMonths > 0 ? utcNow.AddMonths(_expirationPolicy.ExpirationMonths) : (DateTime?)null;

            var entry = await _ledgerRepository.TryCreditAsync(
                new LoyaltyLedgerAppendRequest(
                    account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, rewardPoints, PaymentSourceType,
                    command.PaymentId, $"Trajet payé (règle {rule.Code})", EarningRuleId: rule.Id, ExpirationAtUtc: expirationAtUtc),
                thresholds, utcNow, cancellationToken);

            if (entry is not null)
            {
                await _notificationDispatcher.DispatchAsync(
                    new NotificationRequest(
                        account.UserId, NotificationCategory.Loyalty, "loyalty.points-earned",
                        new Dictionary<string, string> { ["Points"] = rewardPoints.ToString() }, IsMandatory: false, SourceType: PaymentSourceType,
                        SourceId: command.PaymentId),
                    cancellationToken);
            }
        }

        if (statusPoints > 0)
        {
            await _ledgerRepository.TryCreditAsync(
                new LoyaltyLedgerAppendRequest(
                    account.Id, account.UserId, LoyaltyPointType.StatusPoints, LoyaltyLedgerEntryType.Earn, statusPoints, PaymentSourceType,
                    command.PaymentId, $"Trajet payé (règle {rule.Code})", EarningRuleId: rule.Id),
                thresholds, utcNow, cancellationToken);
        }

        if (rewardPoints > 0 || statusPoints > 0)
        {
            await _challengeProgressHandler.Handle(new ProcessChallengeProgressForUserCommand(account.UserId, account.ActorRole), cancellationToken);
        }
    }

    /// <summary>Never queries Subscription tables directly — reads the multiplier from the one stable Limits key the entitlement service exposes, defaulting to 100% (no multiplier) whenever the key, the subscription, or the rule's own opt-in is absent. Bounded to avoid an absurd/misconfigured value inflating awards.</summary>
    private async Task<decimal> ResolveSubscriptionMultiplierAsync(
        Guid userId, UserRole role, bool subscriptionMultiplierAllowed, CancellationToken cancellationToken)
    {
        if (!subscriptionMultiplierAllowed)
        {
            return DefaultMultiplierPercent;
        }

        var entitlement = await _entitlementService.GetEntitlementAsync(userId, role, cancellationToken);

        if (entitlement is null || !entitlement.Limits.TryGetValue(MultiplierLimitKey, out var multiplierPercent))
        {
            return DefaultMultiplierPercent;
        }

        return Math.Clamp(multiplierPercent, 0, MaxMultiplierPercent);
    }

    private async Task TryEvaluateReferralAsync(Guid refereeUserId, CancellationToken cancellationToken)
    {
        var referral = await _referralRepository.GetByRefereeUserIdAsync(refereeUserId, cancellationToken);

        if (referral is null)
        {
            return;
        }

        if (referral.Status == ReferralStatus.PendingActivation)
        {
            await _evaluateReferralActivationHandler.Handle(new EvaluateReferralActivationCommand(referral.Id), cancellationToken);
            referral = await _referralRepository.GetByIdAsync(referral.Id, cancellationToken);

            if (referral is null)
            {
                return;
            }
        }

        var granted = await _referralRewardGranter.TryGrantIfEligibleAsync(referral, cancellationToken);

        if (!granted)
        {
            return;
        }

        var referrerAccount = await _accountRepository.GetByUserIdAsync(referral.ReferrerUserId, cancellationToken);

        if (referrerAccount is not null)
        {
            await _challengeProgressHandler.Handle(
                new ProcessChallengeProgressForUserCommand(referrerAccount.UserId, referrerAccount.ActorRole), cancellationToken);
        }
    }
}
