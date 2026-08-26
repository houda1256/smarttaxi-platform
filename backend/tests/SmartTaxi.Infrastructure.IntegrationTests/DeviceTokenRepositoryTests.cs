using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Infrastructure.Notifications.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves the partial unique index on Token (active rows only) against a real PostgreSQL database.</summary>
[Collection("SharedPostgres")]
public class DeviceTokenRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public DeviceTokenRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TryAddAsync_TwoConcurrentRegistrationsOfSameToken_OnlyOneSucceeds()
    {
        var token = $"push-token-{Guid.NewGuid():N}";
        var first = DeviceToken.Register(Guid.NewGuid(), token, "android", DateTime.UtcNow);
        var second = DeviceToken.Register(Guid.NewGuid(), token, "android", DateTime.UtcNow);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new DeviceTokenRepository(context1).TryAddAsync(first, CancellationToken.None),
            new DeviceTokenRepository(context2).TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }

    [Fact]
    public async Task TryAddAsync_AfterRevokingFirst_AllowsReRegisteringSameToken()
    {
        var token = $"push-token-{Guid.NewGuid():N}";
        var first = DeviceToken.Register(Guid.NewGuid(), token, "ios", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new DeviceTokenRepository(writeContext);
            await repo.TryAddAsync(first, CancellationToken.None);
            first.Revoke(DateTime.UtcNow);
            await repo.UpdateAsync(first, CancellationToken.None);
        }

        var second = DeviceToken.Register(Guid.NewGuid(), token, "ios", DateTime.UtcNow);
        await using var context2 = _fixture.CreateContext();
        var succeeded = await new DeviceTokenRepository(context2).TryAddAsync(second, CancellationToken.None);

        Assert.True(succeeded);
    }

    [Fact]
    public async Task GetActiveForUserAsync_ExcludesRevokedTokens()
    {
        var userId = Guid.NewGuid();
        var active = DeviceToken.Register(userId, $"active-{Guid.NewGuid():N}", "web", DateTime.UtcNow);
        var revoked = DeviceToken.Register(userId, $"revoked-{Guid.NewGuid():N}", "web", DateTime.UtcNow);
        revoked.Revoke(DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new DeviceTokenRepository(writeContext);
            await repo.TryAddAsync(active, CancellationToken.None);
            await repo.TryAddAsync(revoked, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var activeTokens = await new DeviceTokenRepository(readContext).GetActiveForUserAsync(userId, CancellationToken.None);

        Assert.Single(activeTokens, t => t.Id == active.Id);
    }
}
