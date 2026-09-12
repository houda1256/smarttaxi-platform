namespace SmartTaxi.Domain.Administration.Enums;

/// <summary>
/// Exact, closed catalog approved for this pass — not every command in the
/// system is audited, only these 7. AccountUnlocked is deliberately absent:
/// lockout expiry is passive (User.IsLockedOut simply returns false once the
/// clock passes LockedUntilUtc), so no code path ever produces a discrete
/// "unlock" event.
/// </summary>
public enum AuditAction
{
    AdminUserSuspended,
    AdminUserReactivated,
    AdminUserSessionsRevoked,
    AdminUserTwoFactorReset,
    RoleAssigned,
    RoleRemoved,
    AccountLocked
}
