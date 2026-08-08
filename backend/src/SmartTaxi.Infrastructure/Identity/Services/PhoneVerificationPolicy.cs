using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class PhoneVerificationPolicy : IPhoneVerificationPolicy
{
    public int OtpDigits { get; }

    public TimeSpan OtpLifetime { get; }

    public int MaxAttempts { get; }

    public TimeSpan ResendInterval { get; }

    public TimeSpan LockoutDuration { get; }

    public PhoneVerificationPolicy(IOptions<PhoneVerificationOptions> options)
    {
        var value = options.Value;
        OtpDigits = value.OtpDigits;
        OtpLifetime = TimeSpan.FromMinutes(value.OtpLifetimeMinutes);
        MaxAttempts = value.MaxAttempts;
        ResendInterval = TimeSpan.FromSeconds(value.ResendIntervalSeconds);
        LockoutDuration = TimeSpan.FromMinutes(value.LockoutMinutes);
    }
}
