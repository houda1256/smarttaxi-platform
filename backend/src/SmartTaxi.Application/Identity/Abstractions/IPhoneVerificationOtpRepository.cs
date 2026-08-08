using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface IPhoneVerificationOtpRepository
{
    Task AddAsync(PhoneVerificationOtp otp, CancellationToken cancellationToken);

    /// <summary>The most recently issued OTP for this user, regardless of its current state.</summary>
    Task<PhoneVerificationOtp?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<DateTime?> GetLastIssuedAtAsync(Guid userId, CancellationToken cancellationToken);

    Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryConsumeAsync(Guid otpId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments the attempt counter and, if the new count reaches
    /// maxAttempts, locks the OTP until utcNow + lockoutDuration — all in one
    /// SQL statement, so concurrent wrong guesses can't under-count.
    /// </summary>
    Task RecordFailedAttemptAsync(
        Guid otpId, int maxAttempts, DateTime utcNow, TimeSpan lockoutDuration, CancellationToken cancellationToken);
}
