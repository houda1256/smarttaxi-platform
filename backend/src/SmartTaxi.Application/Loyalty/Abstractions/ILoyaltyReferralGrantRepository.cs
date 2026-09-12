using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

/// <summary>
/// Grants a referral reward as one all-or-nothing business transaction: the
/// LoyaltyReferralReward row (which permanently marks the referral rewarded)
/// and both parties' RewardPoints credits either all land together or none of
/// them do — see the Module 7 audit finding #1 this repository exists to fix.
/// Replaces the previous design where the reward row was committed before
/// either credit, which could leave a referral marked rewarded with one or
/// both parties never actually credited and no way to retry.
/// </summary>
public interface ILoyaltyReferralGrantRepository
{
    /// <summary>
    /// Either credit request may be null (a policy can configure zero reward
    /// for one side). Returns false if the reward row already exists (lost
    /// the race to a concurrent grant, or a genuine retry after the referral
    /// was already rewarded), or if any step fails for any other reason — in
    /// every false case, the whole attempt is rolled back and nothing is left
    /// behind, so the referral remains safely retryable.
    /// </summary>
    Task<bool> TryGrantAsync(
        LoyaltyReferralReward reward,
        LoyaltyLedgerAppendRequest? referrerCreditRequest,
        LoyaltyLedgerAppendRequest? refereeCreditRequest,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
