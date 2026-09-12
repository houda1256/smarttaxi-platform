namespace SmartTaxi.Application.Identity.Abstractions;

/// <summary>Mirrors IPhoneVerificationPolicy's exact shape/convention.</summary>
public interface ILoginLockoutPolicy
{
    int MaxFailedAttempts { get; }

    TimeSpan LockoutDuration { get; }
}
