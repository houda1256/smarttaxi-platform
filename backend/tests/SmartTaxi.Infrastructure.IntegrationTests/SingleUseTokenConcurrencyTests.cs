using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real database that the atomic ExecuteUpdateAsync pattern
/// used by every single-use secret (password reset tokens, 2FA challenges,
/// recovery codes) actually prevents double-consumption under concurrent
/// requests — the scenario a load-then-save pattern would be vulnerable to.
/// </summary>
[Collection("SharedPostgres")]
public class SingleUseTokenConcurrencyTests
{
    private readonly SharedPostgresFixture _fixture;

    public SingleUseTokenConcurrencyTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PasswordResetToken_ConcurrentConsumption_OnlyOneAttemptSucceeds()
    {
        var utcNow = DateTime.UtcNow;
        var token = new PasswordResetToken(Guid.NewGuid(), $"reset-hash-{Guid.NewGuid()}", utcNow.AddHours(1), utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new PasswordResetTokenRepository(writeContext).AddAsync(token, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new PasswordResetTokenRepository(contextA);
        var repositoryB = new PasswordResetTokenRepository(contextB);

        var results = await Task.WhenAll(
            repositoryA.TryConsumeAsync(token.Id, utcNow, CancellationToken.None),
            repositoryB.TryConsumeAsync(token.Id, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }

    [Fact]
    public async Task TwoFactorChallenge_ConcurrentConsumption_OnlyOneAttemptSucceeds()
    {
        var utcNow = DateTime.UtcNow;
        var challenge = new TwoFactorChallenge(Guid.NewGuid(), $"challenge-hash-{Guid.NewGuid()}", null, utcNow.AddMinutes(5), utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new TwoFactorChallengeRepository(writeContext).AddAsync(challenge, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new TwoFactorChallengeRepository(contextA);
        var repositoryB = new TwoFactorChallengeRepository(contextB);

        var results = await Task.WhenAll(
            repositoryA.TryConsumeAsync(challenge.Id, utcNow, CancellationToken.None),
            repositoryB.TryConsumeAsync(challenge.Id, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }

    [Fact]
    public async Task TwoFactorRecoveryCode_ConcurrentConsumption_OnlyOneAttemptSucceeds()
    {
        var utcNow = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var code = new TwoFactorRecoveryCode(userId, $"recovery-hash-{Guid.NewGuid()}", utcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new TwoFactorRecoveryCodeRepository(writeContext)
                .AddRangeAsync(new[] { code }, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new TwoFactorRecoveryCodeRepository(contextA);
        var repositoryB = new TwoFactorRecoveryCodeRepository(contextB);

        var results = await Task.WhenAll(
            repositoryA.TryConsumeAsync(code.Id, utcNow, CancellationToken.None),
            repositoryB.TryConsumeAsync(code.Id, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }
}
