using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Infrastructure.Administration.Repositories;
using SmartTaxi.Infrastructure.Administration.Services;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real database the concurrency-safety and atomicity claims
/// of Module 13A: guarded state transitions, cascading session revocation,
/// 2FA-reset cleanup, the append-only AuditLogs trigger, and the shared
/// failed-login counter — the scenarios a load-then-save pattern or an
/// application-only append-only convention would be vulnerable to.
/// </summary>
[Collection("SharedPostgres")]
public class AdminUserManagementIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public AdminUserManagementIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedActiveUserAsync()
    {
        var user = User.Create(
            Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new UserRepository(context).AddAsync(user, CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task TrySuspendAsync_ConcurrentSuspend_OnlyOneAttemptTransitions()
    {
        var userId = await SeedActiveUserAsync();
        var actorId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new AdminUserManagementRepository(
            contextA, new SessionRepository(contextA), new TwoFactorRecoveryCodeRepository(contextA), new NullAuditContextAccessor());
        var repositoryB = new AdminUserManagementRepository(
            contextB, new SessionRepository(contextB), new TwoFactorRecoveryCodeRepository(contextB), new NullAuditContextAccessor());

        var results = await Task.WhenAll(
            repositoryA.TrySuspendAsync(userId, actorId, utcNow, CancellationToken.None),
            repositoryB.TrySuspendAsync(userId, actorId, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.False(reloaded!.IsActive);

        var auditCount = await readContext.AuditLogs.CountAsync(e => e.TargetId == userId && e.Action == AuditAction.AdminUserSuspended);
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task TrySuspendAsync_RevokesAllActiveSessionsAndWritesAuditAtomically()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Start(userId, $"hash-{Guid.NewGuid()}", utcNow.AddHours(1), utcNow.AddDays(30), "device", utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SessionRepository(writeContext).AddAsync(session, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AdminUserManagementRepository(context, new SessionRepository(context), new TwoFactorRecoveryCodeRepository(context), new NullAuditContextAccessor());
            var suspended = await repository.TrySuspendAsync(userId, Guid.NewGuid(), utcNow, CancellationToken.None);
            Assert.True(suspended);
        }

        await using var readContext = _fixture.CreateContext();
        var activeSessions = await new SessionRepository(readContext).GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Empty(activeSessions);

        var auditEntry = await readContext.AuditLogs.SingleAsync(e => e.TargetId == userId && e.Action == AuditAction.AdminUserSuspended);
        Assert.NotNull(auditEntry);
    }

    [Fact]
    public async Task TryReactivateAsync_NeverRestoresRevokedSessions()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Start(userId, $"hash-{Guid.NewGuid()}", utcNow.AddHours(1), utcNow.AddDays(30), "device", utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SessionRepository(writeContext).AddAsync(session, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AdminUserManagementRepository(context, new SessionRepository(context), new TwoFactorRecoveryCodeRepository(context), new NullAuditContextAccessor());
            await repository.TrySuspendAsync(userId, Guid.NewGuid(), utcNow, CancellationToken.None);
            var reactivated = await repository.TryReactivateAsync(userId, Guid.NewGuid(), utcNow, CancellationToken.None);
            Assert.True(reactivated);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.True(reloaded!.IsActive);

        var activeSessions = await new SessionRepository(readContext).GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Empty(activeSessions);
    }

    [Fact]
    public async Task TryRevokeSessionsAsync_IsIdempotent_SecondCallReturnsZeroButStillAudits()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Start(userId, $"hash-{Guid.NewGuid()}", utcNow.AddHours(1), utcNow.AddDays(30), "device", utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SessionRepository(writeContext).AddAsync(session, CancellationToken.None);
        }

        int? first;
        int? second;

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AdminUserManagementRepository(context, new SessionRepository(context), new TwoFactorRecoveryCodeRepository(context), new NullAuditContextAccessor());
            first = await repository.TryRevokeSessionsAsync(userId, Guid.NewGuid(), utcNow, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AdminUserManagementRepository(context, new SessionRepository(context), new TwoFactorRecoveryCodeRepository(context), new NullAuditContextAccessor());
            second = await repository.TryRevokeSessionsAsync(userId, Guid.NewGuid(), utcNow, CancellationToken.None);
        }

        Assert.Equal(1, first);
        Assert.Equal(0, second);

        await using var readContext = _fixture.CreateContext();
        var auditCount = await readContext.AuditLogs.CountAsync(e => e.TargetId == userId && e.Action == AuditAction.AdminUserSessionsRevoked);
        Assert.Equal(2, auditCount);
    }

    [Fact]
    public async Task TryResetTwoFactorAsync_RemovesSecretsAndAllRecoveryCodes()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;

        await using (var context = _fixture.CreateContext())
        {
            var userRepository = new UserRepository(context);
            var user = await userRepository.GetByIdAsync(userId, CancellationToken.None);
            user!.BeginTwoFactorEnrollment("protected:secret", utcNow);
            user.ConfirmTwoFactorEnrollment(utcNow);
            await userRepository.UpdateAsync(user, CancellationToken.None);

            await new TwoFactorRecoveryCodeRepository(context).AddRangeAsync(
                [new TwoFactorRecoveryCode(userId, $"code-hash-{Guid.NewGuid()}", utcNow)], CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new AdminUserManagementRepository(context, new SessionRepository(context), new TwoFactorRecoveryCodeRepository(context), new NullAuditContextAccessor());
            var reset = await repository.TryResetTwoFactorAsync(userId, Guid.NewGuid(), utcNow, CancellationToken.None);
            Assert.True(reset);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.False(reloaded!.TwoFactorEnabled);
        Assert.Null(reloaded.TwoFactorActiveSecretEncrypted);

        var remainingCodes = await new TwoFactorRecoveryCodeRepository(readContext).GetActiveForUserAsync(userId, CancellationToken.None);
        Assert.Empty(remainingCodes);
    }

    [Fact]
    public async Task AuditLogs_RejectsUpdateAndDelete()
    {
        var entry = AuditLogEntry.Create(
            Guid.NewGuid(), AuditAction.RoleAssigned, AuditTargetType.User, Guid.NewGuid(),
            new Dictionary<string, string> { ["role"] = "Driver" }, null, null, null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await writeContext.AuditLogs.AddAsync(entry, CancellationToken.None);
            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var updateContext = _fixture.CreateContext();
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await updateContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"AuditLogs\" SET \"Metadata\" = 'tampered' WHERE \"Id\" = {entry.Id}"));

        await using var deleteContext = _fixture.CreateContext();
        await Assert.ThrowsAsync<PostgresException>(async () =>
            await deleteContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"AuditLogs\" WHERE \"Id\" = {entry.Id}"));
    }

    [Fact]
    public async Task AuditedUserRepository_SaveWithAuditAsync_PersistsBothTheRoleChangeAndTheAuditEntry()
    {
        var userId = await SeedActiveUserAsync();
        AuditLogEntry auditEntry;

        await using (var context = _fixture.CreateContext())
        {
            var user = await new UserRepository(context).GetByIdAsync(userId, CancellationToken.None);
            user!.AssignRole(UserRole.Driver);

            auditEntry = AuditLogEntry.Create(
                Guid.NewGuid(), AuditAction.RoleAssigned, AuditTargetType.User, userId,
                new Dictionary<string, string> { ["role"] = nameof(UserRole.Driver) }, null, null, null, DateTime.UtcNow);

            await new AuditedUserRepository(context).SaveWithAuditAsync(user, auditEntry, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.True(reloaded!.HasRole(UserRole.Driver));

        var persistedEntry = await readContext.AuditLogs.SingleAsync(e => e.Id == auditEntry.Id);
        Assert.Equal(AuditAction.RoleAssigned, persistedEntry.Action);
    }

    [Fact]
    public async Task RecordFailedLoginAttemptAsync_ConcurrentFailures_NoIncrementsAreLost()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;
        const int maxAttempts = 100;
        const int concurrentFailures = 10;

        var contexts = Enumerable.Range(0, concurrentFailures).Select(_ => _fixture.CreateContext()).ToList();

        try
        {
            var tasks = contexts.Select(context =>
                new UserRepository(context).RecordFailedLoginAttemptAsync(
                    userId, maxAttempts, utcNow, TimeSpan.FromMinutes(15), CancellationToken.None));

            await Task.WhenAll(tasks);
        }
        finally
        {
            foreach (var context in contexts)
            {
                await context.DisposeAsync();
            }
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.Equal(concurrentFailures, reloaded!.FailedLoginAttempts);
    }

    [Fact]
    public async Task RecordFailedLoginAttemptAsync_AtThreshold_SetsLockedUntilInTheFuture()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            for (var i = 0; i < 5; i++)
            {
                await repository.RecordFailedLoginAttemptAsync(userId, 5, utcNow, TimeSpan.FromMinutes(15), CancellationToken.None);
            }
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.True(reloaded!.IsLockedOut(utcNow));
        Assert.True(reloaded.LockedUntilUtc > utcNow.AddMinutes(14));
    }

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_ClearsCounterAndLockout()
    {
        var userId = await SeedActiveUserAsync();
        var utcNow = DateTime.UtcNow;

        await using (var context = _fixture.CreateContext())
        {
            var repository = new UserRepository(context);
            for (var i = 0; i < 5; i++)
            {
                await repository.RecordFailedLoginAttemptAsync(userId, 5, utcNow, TimeSpan.FromMinutes(15), CancellationToken.None);
            }

            await repository.ResetFailedLoginAttemptsAsync(userId, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        Assert.Equal(0, reloaded!.FailedLoginAttempts);
        Assert.Null(reloaded.LockedUntilUtc);
    }
}
