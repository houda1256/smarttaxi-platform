namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";

    public int TokenLifetimeHours { get; init; } = 24;

    public int ResendIntervalSeconds { get; init; } = 60;
}
