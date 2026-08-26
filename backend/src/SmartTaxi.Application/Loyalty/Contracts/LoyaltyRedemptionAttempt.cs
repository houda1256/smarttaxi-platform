using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Contracts;

public enum LoyaltyRedemptionAttemptOutcome
{
    /// <summary>A brand-new redemption was created: points debited, usage-limit slot reserved, redemption row persisted.</summary>
    Created,

    /// <summary>The same (UserId, IdempotencyKey) already produced a redemption — no new debit, the existing redemption is returned as-is.</summary>
    Replayed,

    InsufficientBalance,

    UsageLimitReached,
}

public sealed record LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome Outcome, LoyaltyRedemption? Redemption);
