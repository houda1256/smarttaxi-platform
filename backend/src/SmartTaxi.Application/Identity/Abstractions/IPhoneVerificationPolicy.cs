namespace SmartTaxi.Application.Identity.Abstractions;

public interface IPhoneVerificationPolicy
{
    int OtpDigits { get; }

    TimeSpan OtpLifetime { get; }

    int MaxAttempts { get; }

    TimeSpan ResendInterval { get; }

    TimeSpan LockoutDuration { get; }
}
