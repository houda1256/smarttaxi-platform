using SmartTaxi.Application.Loyalty.Contracts;

namespace SmartTaxi.Application.Loyalty.Abstractions;

/// <summary>
/// Performs an entire redemption — request-level idempotency check, points
/// debit, reward usage-limit reservation, and the LoyaltyRedemption row itself
/// — as one all-or-nothing transaction (Module 7 audit fixes #3 and #4).
/// Replaces the previous "debit, then increment, then compensate on failure"
/// sequence: since everything now shares one transaction, a lost usage-limit
/// race rolls the debit back automatically instead of needing a separate
/// compensating credit that could itself fail silently.
/// </summary>
public interface ILoyaltyRedemptionTransactionRepository
{
    Task<LoyaltyRedemptionAttempt> TryRedeemAsync(
        Guid userId, Guid accountId, Guid rewardId, int costInRewardPoints, string idempotencyKey, string reason, DateTime utcNow,
        CancellationToken cancellationToken);
}
