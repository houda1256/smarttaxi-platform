using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartTaxi.API.Tests;

/// <summary>One real Postgres container, migrated once, shared across the whole assembly — mirrors SmartTaxi.Infrastructure.IntegrationTests' SharedPostgresFixture pattern.</summary>
public sealed class SharedApiPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var factory = new SmartTaxiApiFactory(ConnectionString);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("SharedApiPostgres")]
public sealed class SharedApiPostgresCollection : ICollectionFixture<SharedApiPostgresFixture>
{
}
