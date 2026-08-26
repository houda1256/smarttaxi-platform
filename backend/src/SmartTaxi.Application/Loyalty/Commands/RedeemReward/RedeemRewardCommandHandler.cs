using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.RedeemReward;

/// <summary>
/// The debit, the reward's usage-limit reservation, and the LoyaltyRedemption
/// row are all applied as one all-or-nothing transaction by
/// ILoyaltyRedemptionTransactionRepository (Module 7 audit fixes #3 and #4) —
/// a lost usage-limit race rolls the debit back automatically instead of
/// needing a separate compensating refund that could itself fail. A retry
/// with the same IdempotencyKey never re-debits: it replays the original
/// redemption's Id instead. RideDiscount/SubscriptionDiscount rewards are
/// rejected outright: the audit proved Payments has no discount/voucher hook
/// to actually apply them against a fare, so pretending to redeem one would
/// silently lie to the user.
/// </summary>
public sealed class RedeemRewardCommandHandler : ICommandHandler<RedeemRewardCommand, Result<Guid>>
{
    private const string NotFoundError = "Récompense introuvable.";
    private const string NotAvailableError = "Cette récompense n'est pas disponible.";
    private const string NotEligibleForRoleError = "Cette récompense n'est pas disponible pour votre rôle.";
    private const string NotExecutableError = "Cette récompense nécessite une intégration de paiement qui n'existe pas encore et ne peut pas être échangée pour le moment.";
    private const string NoAccountError = "Aucun compte fidélité trouvé.";
    private const string InsufficientBalanceError = "Solde de points insuffisant.";
    private const string RedemptionLimitReachedError = "La limite d'utilisation de cette récompense a été atteinte.";
    private const string MissingIdempotencyKeyError = "Une clé d'idempotence est requise.";

    private readonly ILoyaltyRewardRepository _rewardRepository;
    private readonly ILoyaltyAccountRepository _accountRepository;
    private readonly ILoyaltyRedemptionTransactionRepository _redemptionTransactionRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RedeemRewardCommandHandler(
        ILoyaltyRewardRepository rewardRepository, ILoyaltyAccountRepository accountRepository,
        ILoyaltyRedemptionTransactionRepository redemptionTransactionRepository, INotificationDispatcher notificationDispatcher)
    {
        _rewardRepository = rewardRepository;
        _accountRepository = accountRepository;
        _redemptionTransactionRepository = redemptionTransactionRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<Guid>> Handle(RedeemRewardCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return Result<Guid>.Failure(MissingIdempotencyKeyError, ErrorType.Validation);
        }

        var reward = await _rewardRepository.GetByIdAsync(command.RewardId, cancellationToken);

        if (reward is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        if (!reward.IsAvailable(utcNow))
        {
            return Result<Guid>.Failure(NotAvailableError, ErrorType.Conflict);
        }

        if (!reward.IsExecutable)
        {
            return Result<Guid>.Failure(NotExecutableError, ErrorType.Validation);
        }

        var account = await _accountRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (account is null)
        {
            return Result<Guid>.Failure(NoAccountError, ErrorType.NotFound);
        }

        if (!reward.IsAvailableForRole(account.ActorRole))
        {
            return Result<Guid>.Failure(NotEligibleForRoleError, ErrorType.Forbidden);
        }

        var attempt = await _redemptionTransactionRepository.TryRedeemAsync(
            account.UserId, account.Id, reward.Id, reward.CostInRewardPoints, command.IdempotencyKey.Trim(), $"Échange : {reward.Name}",
            utcNow, cancellationToken);

        switch (attempt.Outcome)
        {
            case LoyaltyRedemptionAttemptOutcome.InsufficientBalance:
                return Result<Guid>.Failure(InsufficientBalanceError, ErrorType.Conflict);

            case LoyaltyRedemptionAttemptOutcome.UsageLimitReached:
                return Result<Guid>.Failure(RedemptionLimitReachedError, ErrorType.Conflict);

            case LoyaltyRedemptionAttemptOutcome.Replayed when attempt.Redemption is null:
                // Lost a race on the same idempotency key and the winner hasn't become visible yet —
                // safe for the client to retry the exact same request.
                return Result<Guid>.Failure(RedemptionLimitReachedError, ErrorType.Conflict);

            case LoyaltyRedemptionAttemptOutcome.Replayed:
                return Result<Guid>.Success(attempt.Redemption!.Id);

            default:
                await _notificationDispatcher.DispatchAsync(
                    new NotificationRequest(
                        account.UserId, NotificationCategory.Loyalty, "loyalty.reward-redeemed",
                        new Dictionary<string, string> { ["RewardName"] = reward.Name, ["Points"] = reward.CostInRewardPoints.ToString() },
                        IsMandatory: false, SourceType: "Redemption", SourceId: attempt.Redemption!.Id),
                    cancellationToken);

                return Result<Guid>.Success(attempt.Redemption!.Id);
        }
    }
}
