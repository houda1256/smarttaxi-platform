using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface ITwoFactorRecoveryCodeRepository
{
    Task AddRangeAsync(IReadOnlyCollection<TwoFactorRecoveryCode> codes, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TwoFactorRecoveryCode>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> TryConsumeAsync(Guid codeId, DateTime utcNow, CancellationToken cancellationToken);

    Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
