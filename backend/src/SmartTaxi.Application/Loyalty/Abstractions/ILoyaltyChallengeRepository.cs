using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyChallengeRepository
{
    Task<LoyaltyChallenge?> GetByIdAsync(Guid challengeId, CancellationToken cancellationToken);

    Task<LoyaltyChallenge?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyChallenge>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyChallenge>> GetActiveForRoleAsync(UserRole role, DateTime utcNow, CancellationToken cancellationToken);

    Task AddAsync(LoyaltyChallenge challenge, CancellationToken cancellationToken);

    Task UpdateAsync(LoyaltyChallenge challenge, CancellationToken cancellationToken);
}
