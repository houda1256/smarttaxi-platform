using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;
using SmartTaxi.Infrastructure.Analytics.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves, against real PostgreSQL, that TryClaimAsync's atomic conditional UPDATE is the true race-safe enforcement point — two concurrent processors can never both win the claim for the same due occurrence.</summary>
[Collection("SharedPostgres")]
public class ScheduledReportRepositoryIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public ScheduledReportRepositoryIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> CreateDueDefinitionAsync()
    {
        var definition = ScheduledReportDefinition.Create(
            ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(-1));

        await using var context = _fixture.CreateContext();
        await new ScheduledReportRepository(context).TryAddAsync(definition, CancellationToken.None);
        return definition.Id;
    }

    [Fact]
    public async Task TwoConcurrentClaims_OnlyOneSucceeds()
    {
        var id = await CreateDueDefinitionAsync();
        var utcNow = DateTime.UtcNow;

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new ScheduledReportRepository(context1).TryClaimAsync(id, utcNow, CancellationToken.None),
            new ScheduledReportRepository(context2).TryClaimAsync(id, utcNow, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task Finalize_WithWrongClaimTimestamp_Fails()
    {
        var id = await CreateDueDefinitionAsync();
        var claimedAtUtc = DateTime.UtcNow;

        await using (var context = _fixture.CreateContext())
        {
            await new ScheduledReportRepository(context).TryClaimAsync(id, claimedAtUtc, CancellationToken.None);
        }

        await using var context2 = _fixture.CreateContext();
        var wrongTimestamp = claimedAtUtc.AddSeconds(1);
        var finalized = await new ScheduledReportRepository(context2).TryFinalizeAsync(
            id, wrongTimestamp, DateTime.UtcNow.AddDays(1), DateTime.UtcNow, CancellationToken.None);

        Assert.False(finalized);
    }

    [Fact]
    public async Task Finalize_WithCorrectClaimTimestamp_ClearsClaimAndAdvancesSchedule()
    {
        var id = await CreateDueDefinitionAsync();
        var claimedAtUtc = DateTime.UtcNow;
        // Truncated to whole seconds to avoid a sub-microsecond precision mismatch when the value
        // round-trips through PostgreSQL's timestamptz (microsecond precision) vs. .NET DateTime (tick precision).
        var nextRunAtUtc = new DateTime(DateTime.UtcNow.AddDays(1).Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        await using (var context = _fixture.CreateContext())
        {
            await new ScheduledReportRepository(context).TryClaimAsync(id, claimedAtUtc, CancellationToken.None);
        }

        await using var context2 = _fixture.CreateContext();
        var repository = new ScheduledReportRepository(context2);
        var finalized = await repository.TryFinalizeAsync(id, claimedAtUtc, nextRunAtUtc, DateTime.UtcNow, CancellationToken.None);

        Assert.True(finalized);
        var reloaded = await repository.GetByIdAsync(id, CancellationToken.None);
        Assert.Null(reloaded!.ProcessingClaimedAtUtc);
        Assert.Equal(nextRunAtUtc, reloaded.NextRunAtUtc);
        Assert.NotNull(reloaded.LastProcessedAtUtc);
    }

    [Fact]
    public async Task GetDueIdsAsync_ExcludesFreshlyClaimedDefinition()
    {
        var id = await CreateDueDefinitionAsync();
        var utcNow = DateTime.UtcNow;

        await using (var context = _fixture.CreateContext())
        {
            await new ScheduledReportRepository(context).TryClaimAsync(id, utcNow, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var dueIds = await new ScheduledReportRepository(readContext).GetDueIdsAsync(utcNow.AddSeconds(1), CancellationToken.None);

        Assert.DoesNotContain(id, dueIds);
    }

    [Fact]
    public async Task GetDueIdsAsync_IncludesStaleClaimedDefinition()
    {
        var id = await CreateDueDefinitionAsync();
        var staleClaimTime = DateTime.UtcNow - IScheduledReportRepository.StaleClaimThreshold - TimeSpan.FromMinutes(1);

        await using (var context = _fixture.CreateContext())
        {
            await new ScheduledReportRepository(context).TryClaimAsync(id, staleClaimTime, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var dueIds = await new ScheduledReportRepository(readContext).GetDueIdsAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Contains(id, dueIds);
    }
}
