using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePhoneVerificationPolicy : IPhoneVerificationPolicy
{
    public int OtpDigits { get; init; } = 6;

    public TimeSpan OtpLifetime { get; init; } = TimeSpan.FromMinutes(5);

    public int MaxAttempts { get; init; } = 5;

    public TimeSpan ResendInterval { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);
}
