using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface ITwoFactorChallengeRepository
{
    Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken);

    Task<TwoFactorChallenge?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> TryConsumeAsync(Guid challengeId, DateTime utcNow, CancellationToken cancellationToken);
}
