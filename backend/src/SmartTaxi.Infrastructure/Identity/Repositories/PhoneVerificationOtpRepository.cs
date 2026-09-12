using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class PhoneVerificationOtpRepository : IPhoneVerificationOtpRepository
{
    private readonly ApplicationDbContext _context;

    public PhoneVerificationOtpRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PhoneVerificationOtp otp, CancellationToken cancellationToken)
    {
        await _context.PhoneVerificationOtps.AddAsync(otp, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<PhoneVerificationOtp?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.PhoneVerificationOtps
            .Where(otp => otp.UserId == userId)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<DateTime?> GetLastIssuedAtAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.PhoneVerificationOtps
            .Where(otp => otp.UserId == userId)
            .OrderByDescending(otp => otp.CreatedAt)
            .Select(otp => (DateTime?)otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken) =>
        _context.PhoneVerificationOtps
            .Where(otp => otp.UserId == userId && otp.ConsumedAt == null && otp.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(otp => otp.ExpiresAt, utcNow), cancellationToken);

    public async Task<bool> TryConsumeAsync(Guid otpId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.PhoneVerificationOtps
            .Where(otp => otp.Id == otpId && otp.ConsumedAt == null && otp.ExpiresAt > utcNow
                && (otp.LockedUntil == null || otp.LockedUntil <= utcNow))
            .ExecuteUpdateAsync(setters => setters.SetProperty(otp => otp.ConsumedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public Task RecordFailedAttemptAsync(
        Guid otpId, int maxAttempts, DateTime utcNow, TimeSpan lockoutDuration, CancellationToken cancellationToken)
    {
        var lockedUntilIfThresholdReached = utcNow.Add(lockoutDuration);

        return _context.PhoneVerificationOtps
            .Where(otp => otp.Id == otpId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(otp => otp.AttemptCount, otp => otp.AttemptCount + 1)
                    .SetProperty(
                        otp => otp.LockedUntil,
                        otp => otp.AttemptCount + 1 >= maxAttempts ? lockedUntilIfThresholdReached : otp.LockedUntil),
                cancellationToken);
    }
}
