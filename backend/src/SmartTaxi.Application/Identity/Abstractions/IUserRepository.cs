using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments FailedLoginAttempts and, if the new count
    /// reaches maxAttempts, sets LockedUntilUtc to utcNow + lockoutDuration —
    /// all in one SQL statement, mirroring IPhoneVerificationOtpRepository.
    /// RecordFailedAttemptAsync exactly, so concurrent wrong guesses can't
    /// under-count. Returns the resulting attempt count so the caller can
    /// detect the exact transition into a locked state (newCount == maxAttempts)
    /// without a separate read.
    /// </summary>
    Task<int> RecordFailedLoginAttemptAsync(
        Guid userId, int maxAttempts, DateTime utcNow, TimeSpan lockoutDuration, CancellationToken cancellationToken);

    /// <summary>Called only after a fully completed successful authentication (password alone when 2FA is off; the 2FA challenge success when it's on) — never after password success while a 2FA challenge is still pending.</summary>
    Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken);
}
