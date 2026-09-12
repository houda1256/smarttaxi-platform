namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class LoginLockoutOptions
{
    public const string SectionName = "LoginLockout";

    public int MaxFailedAttempts { get; init; } = 5;

    public int LockoutMinutes { get; init; } = 15;
}
