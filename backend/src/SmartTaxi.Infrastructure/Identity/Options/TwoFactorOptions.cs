namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class TwoFactorOptions
{
    public const string SectionName = "TwoFactor";

    public int RecoveryCodeCount { get; init; } = 10;

    public int ChallengeTokenLifetimeMinutes { get; init; } = 5;

    public int TotpDigits { get; init; } = 6;

    public int TotpStepSeconds { get; init; } = 30;

    public int TotpDriftSteps { get; init; } = 1;

    public string Issuer { get; init; } = "SmartTaxi";
}
