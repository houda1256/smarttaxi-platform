namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int TokenLifetimeHours { get; init; } = 1;
}
