using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Administration.Repositories;

/// <summary>
/// Each guard uses the same atomic conditional ExecuteUpdateAsync idiom used
/// everywhere else in this codebase for concurrency-safe state transitions
/// (see SessionRepository/PhoneVerificationOtpRepository) rather than a
/// load-then-call-domain-method-then-save round trip, since User carries no
/// concurrency token. The WHERE-guarded update mirrors exactly the field
/// changes User.Deactivate()/Activate()/DisableTwoFactor() apply.
/// </summary>
internal sealed class AdminUserManagementRepository : IAdminUserManagementRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITwoFactorRecoveryCodeRepository _recoveryCodeRepository;

    public AdminUserManagementRepository(
        ApplicationDbContext context, ISessionRepository sessionRepository, ITwoFactorRecoveryCodeRepository recoveryCodeRepository)
    {
        _context = context;
        _sessionRepository = sessionRepository;
        _recoveryCodeRepository = recoveryCodeRepository;
    }

    public async Task<bool> TrySuspendAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var affected = await _context.Users
            .Where(u => u.Id == targetUserId && u.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsActive, false), cancellationToken);

        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _sessionRepository.RevokeAllActiveSessionsAsync(targetUserId, SessionRevocationReason.AdminAction, utcNow, cancellationToken);

        var auditEntry = AuditLogEntry.Create(
            actorUserId, AuditAction.AdminUserSuspended, AuditTargetType.User, targetUserId, null, null, null, null, utcNow);
        await _context.AuditLogs.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryReactivateAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var affected = await _context.Users
            .Where(u => u.Id == targetUserId && !u.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsActive, true), cancellationToken);

        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var auditEntry = AuditLogEntry.Create(
            actorUserId, AuditAction.AdminUserReactivated, AuditTargetType.User, targetUserId, null, null, null, null, utcNow);
        await _context.AuditLogs.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int?> TryRevokeSessionsAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var exists = await _context.Users.AnyAsync(u => u.Id == targetUserId, cancellationToken);

        if (!exists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var revokedCount = await _sessionRepository.RevokeAllActiveSessionsAsync(
            targetUserId, SessionRevocationReason.AdminAction, utcNow, cancellationToken);

        var auditEntry = AuditLogEntry.Create(
            actorUserId, AuditAction.AdminUserSessionsRevoked, AuditTargetType.User, targetUserId,
            new Dictionary<string, string> { ["revokedCount"] = revokedCount.ToString() }, null, null, null, utcNow);
        await _context.AuditLogs.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return revokedCount;
    }

    public async Task<bool> TryResetTwoFactorAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var affected = await _context.Users
            .Where(u => u.Id == targetUserId && u.TwoFactorEnabled)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.TwoFactorEnabled, false)
                    .SetProperty(u => u.TwoFactorActiveSecretEncrypted, (string?)null)
                    .SetProperty(u => u.TwoFactorConfirmedAt, (DateTime?)null)
                    .SetProperty(u => u.TwoFactorPendingSecretEncrypted, (string?)null)
                    .SetProperty(u => u.TwoFactorPendingSecretCreatedAt, (DateTime?)null),
                cancellationToken);

        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _recoveryCodeRepository.DeleteAllForUserAsync(targetUserId, cancellationToken);

        var auditEntry = AuditLogEntry.Create(
            actorUserId, AuditAction.AdminUserTwoFactorReset, AuditTargetType.User, targetUserId, null, null, null, null, utcNow);
        await _context.AuditLogs.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
