namespace SmartTaxi.Application.Administration.Abstractions;

/// <summary>
/// Each method atomically composes the business mutation, its side effects
/// (session revocation, recovery-code invalidation), and the corresponding
/// AuditLogEntry insert in a single transaction — a committed audit entry
/// describing an action that rolled back, or a committed action with no
/// audit entry, are both unacceptable per the approved design. All four
/// operations reuse already-existing capabilities (User.Deactivate/Activate/
/// DisableTwoFactor, ISessionRepository.RevokeAllActiveSessionsAsync,
/// ITwoFactorRecoveryCodeRepository.DeleteAllForUserAsync) — nothing here
/// reimplements them.
/// </summary>
public interface IAdminUserManagementRepository
{
    /// <summary>Guarded WHERE IsActive == true. Also revokes every active session for the target in the same transaction. Returns false if the target does not exist or is already suspended.</summary>
    Task<bool> TrySuspendAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded WHERE IsActive == false. Never restores sessions. Returns false if the target does not exist or is already active.</summary>
    Task<bool> TryReactivateAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Idempotent by construction (bulk conditional update) — always writes an audit entry, even when zero sessions were revoked. Returns the number of sessions actually revoked, or null if the target does not exist.</summary>
    Task<int?> TryRevokeSessionsAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded WHERE TwoFactorEnabled == true. Also deletes every recovery code for the target in the same transaction. Returns false if the target does not exist or 2FA is already disabled.</summary>
    Task<bool> TryResetTwoFactorAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken);
}
