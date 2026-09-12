using Microsoft.EntityFrameworkCore;
using SmartTaxi.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Starts one real PostgreSQL container, applies every migration to the latest
/// version, and shares it across all tests in the "SharedPostgres" collection.
/// Each test uses its own randomly-generated ids/emails so they don't interfere
/// with each other despite sharing the schema.
/// </summary>
public sealed class SharedPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}

[CollectionDefinition("SharedPostgres")]
public sealed class SharedPostgresCollection : ICollectionFixture<SharedPostgresFixture>
{
}
