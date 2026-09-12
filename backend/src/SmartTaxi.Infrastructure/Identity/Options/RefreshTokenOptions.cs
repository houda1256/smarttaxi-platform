namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    /// <summary>Per-token sliding lifetime, renewed on every successful rotation.</summary>
    public int LifetimeMinutes { get; init; } = 43200; // 30 days

    /// <summary>Absolute session lifetime, set once at login and never extended by rotation.</summary>
    public int SessionLifetimeDays { get; init; } = 90;
}
