using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);

    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryConsumeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken);
}
