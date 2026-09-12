using SmartTaxi.Domain.Identity.Referrals.Entities;

namespace SmartTaxi.Application.Identity.Referrals.Abstractions;

public interface IReferralRepository
{
    /// <summary>
    /// Returns false (instead of throwing) if a concurrent insert already gave
    /// this referee a sponsor — the unique index on RefereeUserId is the real
    /// enforcement point, this just surfaces the outcome without an exception.
    /// </summary>
    Task<bool> AddAsync(Referral referral, CancellationToken cancellationToken);

    Task<Referral?> GetByIdAsync(Guid referralId, CancellationToken cancellationToken);

    /// <summary>Used to enforce "one sponsor only" — a referee can never have more than one referral row.</summary>
    Task<Referral?> GetByRefereeUserIdAsync(Guid refereeUserId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Referral>> GetForReferrerAsync(Guid referrerUserId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Referral>> GetPendingActivationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Referrals Identity has already marked RewardEligible — added for Module 7
    /// (Loyalty) so it can sweep for referrals to reward without ever reading
    /// Identity's Referral table directly through anything but this repository
    /// interface. Identity's own referral business rules are unchanged; this is
    /// purely an additional read.
    /// </summary>
    Task<IReadOnlyCollection<Referral>> GetRewardEligibleAsync(CancellationToken cancellationToken);

    Task<bool> TryActivateAsync(Guid referralId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryInvalidateAsync(Guid referralId, DateTime utcNow, CancellationToken cancellationToken);
}
