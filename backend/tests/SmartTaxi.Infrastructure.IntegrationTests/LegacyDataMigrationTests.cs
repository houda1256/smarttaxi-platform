using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;
using SmartTaxi.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves two backward-compatibility claims against a real database: a user row
/// created under the original single-role schema (2a) keeps their role after the
/// AddMultiRoleAndPermissions migration, and becomes IsActive=true (2b) after the
/// AddSessionsAndRefreshTokens migration — never silently deactivated. Uses its
/// own dedicated container (not the shared fixture) because it deliberately
/// manipulates migration state.
/// </summary>
public class LegacyDataMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Migration_PreservesExistingUsersRole()
    {
        var legacyUserId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

            // Apply only the original schema (single scalar Users.Role column).
            await migrator.MigrateAsync("20260721101836_InitialCreate");

            // Insert a row exactly as it would have existed before this
            // sub-slice — the scenario the migration's data-copy step must handle.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Users" ("Id", "Email", "PasswordHash", "Role")
                VALUES ({legacyUserId}, 'legacy-user@example.com', 'legacy-hash', 'Admin')
                """);

            // Apply the rest of the migrations, including AddMultiRoleAndPermissions.
            await migrator.MigrateAsync();
        }

        await using var readContext = CreateContext();
        var repository = new UserRepository(readContext);
        var migratedUser = await repository.GetByIdAsync(legacyUserId, CancellationToken.None);

        Assert.NotNull(migratedUser);
        Assert.True(migratedUser!.HasRole(UserRole.Admin));
        Assert.Single(migratedUser.Roles);
        // 2b's AddSessionsAndRefreshTokens migration must default this pre-existing
        // user to active, not deactivate them — this was caught and fixed during
        // development (the first scaffolded migration defaulted to false).
        Assert.True(migratedUser.IsActive);
    }

    [Fact]
    public async Task Migration_Add2cIdentitySecurityFeatures_DoesNotDisruptExistingUsersOrSessions()
    {
        var legacyUserId = Guid.NewGuid();
        var legacySessionId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

            // Apply everything up to (but not including) the 2c migration — the
            // schema as it existed at the end of sub-slice 2b.
            await migrator.MigrateAsync("20260729234615_AddSessionsAndRefreshTokens");

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Users" ("Id", "Email", "PasswordHash", "IsActive")
                VALUES ({legacyUserId}, 'pre-2c-user@example.com', 'legacy-hash', true)
                """);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "UserRoles" ("UserId", "Role")
                VALUES ({legacyUserId}, 'Customer')
                """);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Sessions" ("Id", "UserId", "CreatedAt", "LastActivityAt", "ExpiresAt", "ConcurrencyStamp")
                VALUES ({legacySessionId}, {legacyUserId}, now(), now(), now() + interval '90 days', {Guid.NewGuid()})
                """);

            // Apply the 2c migration on top of this pre-existing data.
            await migrator.MigrateAsync();
        }

        await using var readContext = CreateContext();
        var userRepository = new UserRepository(readContext);
        var migratedUser = await userRepository.GetByIdAsync(legacyUserId, CancellationToken.None);

        Assert.NotNull(migratedUser);
        // New nullable columns must default to "not verified/not enrolled" for
        // pre-existing rows, never break the read or silently flip a security flag on.
        Assert.False(migratedUser!.TwoFactorEnabled);
        Assert.Null(migratedUser.EmailVerifiedAt);
        Assert.Null(migratedUser.PhoneVerifiedAt);
        Assert.Null(migratedUser.PhoneNumber);

        var sessionRepository = new SessionRepository(readContext);
        var migratedSession = await sessionRepository.GetByIdAsync(legacySessionId, CancellationToken.None);
        Assert.NotNull(migratedSession);
        Assert.Null(migratedSession!.RevokedAt);
    }
}
