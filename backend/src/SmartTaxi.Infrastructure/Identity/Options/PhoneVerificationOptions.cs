namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class PhoneVerificationOptions
{
    public const string SectionName = "PhoneVerification";

    public int OtpDigits { get; init; } = 6;

    public int OtpLifetimeMinutes { get; init; } = 5;

    public int MaxAttempts { get; init; } = 5;

    public int ResendIntervalSeconds { get; init; } = 60;

    public int LockoutMinutes { get; init; } = 15;
}
