using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken);

    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<DateTime?> GetLastIssuedAtAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Immediately expires every currently-valid token for this user (atomic bulk update).</summary>
    Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically marks the token consumed only if it is still unconsumed and
    /// unexpired. Returns false if it was already consumed/expired/gone —
    /// this is what makes "cannot consume the same token twice" race-safe.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken);
}
