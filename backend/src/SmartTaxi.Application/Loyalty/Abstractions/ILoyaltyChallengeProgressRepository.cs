using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyChallengeProgressRepository
{
    Task<LoyaltyChallengeProgress?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyChallengeProgress>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>False when the unique (UserId, ChallengeId) index rejects a concurrent duplicate start — a user can only ever have one progress row per challenge.</summary>
    Task<bool> TryAddAsync(LoyaltyChallengeProgress progress, CancellationToken cancellationToken);

    Task UpdateAsync(LoyaltyChallengeProgress progress, CancellationToken cancellationToken);
}
