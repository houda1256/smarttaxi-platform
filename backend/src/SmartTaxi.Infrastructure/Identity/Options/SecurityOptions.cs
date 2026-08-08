namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Whether an authenticated password change revokes every session other than
    /// the one that performed the change. Password *reset* (the unauthenticated
    /// forgot-password flow) always revokes all sessions unconditionally and is
    /// not affected by this setting.
    /// </summary>
    public bool RevokeOtherSessionsOnPasswordChange { get; init; } = true;
}
