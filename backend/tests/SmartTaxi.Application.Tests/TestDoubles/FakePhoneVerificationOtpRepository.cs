using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePhoneVerificationOtpRepository : IPhoneVerificationOtpRepository
{
    private readonly Dictionary<Guid, PhoneVerificationOtp> _otpsById = new();

    public Task AddAsync(PhoneVerificationOtp otp, CancellationToken cancellationToken)
    {
        _otpsById[otp.Id] = otp;
        return Task.CompletedTask;
    }

    public Task<PhoneVerificationOtp?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var latest = _otpsById.Values
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<DateTime?> GetLastIssuedAtAsync(Guid userId, CancellationToken cancellationToken)
    {
        var lastIssuedAt = _otpsById.Values
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (DateTime?)o.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(lastIssuedAt);
    }

    public Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        foreach (var otp in _otpsById.Values.Where(o => o.UserId == userId && o.IsValid(utcNow)))
        {
            SetProperty(otp, nameof(PhoneVerificationOtp.ExpiresAt), utcNow);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryConsumeAsync(Guid otpId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_otpsById.TryGetValue(otpId, out var otp) || !otp.IsValid(utcNow))
        {
            return Task.FromResult(false);
        }

        SetProperty(otp, nameof(PhoneVerificationOtp.ConsumedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task RecordFailedAttemptAsync(
        Guid otpId, int maxAttempts, DateTime utcNow, TimeSpan lockoutDuration, CancellationToken cancellationToken)
    {
        if (!_otpsById.TryGetValue(otpId, out var otp))
        {
            return Task.CompletedTask;
        }

        var newCount = otp.AttemptCount + 1;
        SetProperty(otp, nameof(PhoneVerificationOtp.AttemptCount), newCount);

        if (newCount >= maxAttempts)
        {
            SetProperty(otp, nameof(PhoneVerificationOtp.LockedUntil), (DateTime?)utcNow.Add(lockoutDuration));
        }

        return Task.CompletedTask;
    }

    public int Count => _otpsById.Count;

    // PhoneVerificationOtp has no public mutators by design — real mutation
    // happens via atomic repository-level SQL in production; this fake uses
    // reflection to simulate that same atomicity in memory for tests.
    private static void SetProperty(PhoneVerificationOtp otp, string propertyName, object? value) =>
        typeof(PhoneVerificationOtp).GetProperty(propertyName)!.SetValue(otp, value);
}
