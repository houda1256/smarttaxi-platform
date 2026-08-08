using Npgsql;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;
using SmartTaxi.Infrastructure.Identity.Services;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class SessionRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public SessionRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_NeverPersistsTheRawRefreshTokenValue()
    {
        var hasher = new RefreshTokenHasher();
        const string rawToken = "raw-token-that-must-never-appear-in-the-database";
        var tokenHash = hasher.Hash(rawToken);
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Start(Guid.NewGuid(), tokenHash, utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(session, CancellationToken.None);
        }

        await using var rawConnection = new NpgsqlConnection(_fixture.ConnectionString);
        await rawConnection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT \"TokenHash\" FROM \"RefreshTokens\" WHERE \"SessionId\" = @sessionId", rawConnection);
        command.Parameters.AddWithValue("sessionId", session.Id);
        var storedValue = (string)(await command.ExecuteScalarAsync())!;

        Assert.Equal(tokenHash, storedValue);
        Assert.NotEqual(rawToken, storedValue);
    }

    [Fact]
    public async Task RotateToken_ReusedOldToken_RevokesSessionInTheDatabase()
    {
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Start(Guid.NewGuid(), "reuse-test-hash-1", utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(session, CancellationToken.None);
        }

        // First, legitimate rotation.
        await using (var rotateContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(rotateContext);
            var loaded = await repository.GetByIdAsync(session.Id, CancellationToken.None);
            var rotation = loaded!.RotateToken("reuse-test-hash-1", "reuse-test-hash-2", utcNow.AddMinutes(60), utcNow.AddMinutes(5));
            Assert.Equal(TokenRotationOutcome.Success, rotation.Outcome);
            var saved = await repository.UpdateAsync(loaded, CancellationToken.None);
            Assert.True(saved);
        }

        // Reuse of the now-replaced original token.
        await using (var reuseContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(reuseContext);
            var loaded = await repository.GetByIdAsync(session.Id, CancellationToken.None);
            var result = loaded!.RotateToken("reuse-test-hash-1", "reuse-test-hash-3", utcNow.AddMinutes(90), utcNow.AddMinutes(10));
            Assert.Equal(TokenRotationOutcome.ReuseDetected, result.Outcome);
            await repository.UpdateAsync(loaded, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new SessionRepository(readContext);
        var finalSession = await readRepository.GetByIdAsync(session.Id, CancellationToken.None);

        Assert.NotNull(finalSession!.RevokedAt);
        Assert.Equal(SessionRevocationReason.ReuseDetected, finalSession.RevokedReason);
    }

    [Fact]
    public async Task ConcurrentRotation_OnTheSameToken_OnlyOneAttemptSucceeds()
    {
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Start(Guid.NewGuid(), "concurrency-test-hash", utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(session, CancellationToken.None);
        }

        // Load the same row through two independent contexts first, so both see
        // the same starting xmin — then race their saves against each other.
        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new SessionRepository(contextA);
        var repositoryB = new SessionRepository(contextB);

        var sessionA = await repositoryA.GetByIdAsync(session.Id, CancellationToken.None);
        var sessionB = await repositoryB.GetByIdAsync(session.Id, CancellationToken.None);

        var rotationA = sessionA!.RotateToken("concurrency-test-hash", "winner-or-loser-a", utcNow.AddHours(1), utcNow.AddMinutes(1));
        var rotationB = sessionB!.RotateToken("concurrency-test-hash", "winner-or-loser-b", utcNow.AddHours(1), utcNow.AddMinutes(1));

        Assert.Equal(TokenRotationOutcome.Success, rotationA.Outcome);
        Assert.Equal(TokenRotationOutcome.Success, rotationB.Outcome);

        var saveResults = await Task.WhenAll(
            repositoryA.UpdateAsync(sessionA, CancellationToken.None),
            repositoryB.UpdateAsync(sessionB, CancellationToken.None));

        Assert.Single(saveResults, succeeded => succeeded);
    }

    [Fact]
    public async Task RevokeAllActiveSessionsAsync_WithExceptSessionId_KeepsThatSessionActive_MatchingPasswordChangePolicy()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;
        var keptSession = UserSession.Start(userId, $"kept-{Guid.NewGuid()}", utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);
        var otherSession = UserSession.Start(userId, $"other-{Guid.NewGuid()}", utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(keptSession, CancellationToken.None);
            await repository.AddAsync(otherSession, CancellationToken.None);
        }

        await using (var revokeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(revokeContext);
            var revokedCount = await repository.RevokeAllActiveSessionsAsync(
                userId, SessionRevocationReason.PasswordChanged, utcNow, CancellationToken.None, exceptSessionId: keptSession.Id);

            Assert.Equal(1, revokedCount);
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new SessionRepository(readContext);
        var active = await readRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);

        Assert.Single(active);
        Assert.Equal(keptSession.Id, active.Single().Id);
    }

    [Fact]
    public async Task RevokeAllActiveSessionsAsync_WithoutExceptSessionId_RevokesEverySession_MatchingPasswordResetPolicy()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;
        var sessionOne = UserSession.Start(userId, $"reset-one-{Guid.NewGuid()}", utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);
        var sessionTwo = UserSession.Start(userId, $"reset-two-{Guid.NewGuid()}", utcNow.AddMinutes(30), utcNow.AddDays(90), null, utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(sessionOne, CancellationToken.None);
            await repository.AddAsync(sessionTwo, CancellationToken.None);
        }

        await using (var revokeContext = _fixture.CreateContext())
        {
            var repository = new SessionRepository(revokeContext);
            var revokedCount = await repository.RevokeAllActiveSessionsAsync(
                userId, SessionRevocationReason.PasswordReset, utcNow, CancellationToken.None);

            Assert.Equal(2, revokedCount);
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new SessionRepository(readContext);
        var active = await readRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);

        Assert.Empty(active);
    }
}
