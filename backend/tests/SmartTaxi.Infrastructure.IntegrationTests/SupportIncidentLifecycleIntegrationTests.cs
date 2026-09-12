using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Support;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Support.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves, against real PostgreSQL, that the partial unique index on (SourceType, SourceId) is the true race-safe enforcement point for ISupportIncidentReporter's idempotency guarantee — something an in-memory fake can only simulate, never actually prove.</summary>
[Collection("SharedPostgres")]
public class SupportIncidentLifecycleIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public SupportIncidentLifecycleIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static SupportIncidentReportRequest BuildRequest(string? sourceType, Guid? sourceId) => new(
        SupportIncidentType.RideIncident, SupportIncidentSeverity.Moderate, "Titre", "Description", Guid.NewGuid(), null, null, null, null,
        DateTime.UtcNow, sourceType, sourceId);

    private async Task<SupportIncident> CreateIncidentAsync()
    {
        var incident = SupportIncident.Create(
            SupportIncidentType.RideIncident, SupportIncidentSeverity.Moderate, "Titre", "Description", Guid.NewGuid(), null, null, null,
            null, DateTime.UtcNow, null, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new SupportIncidentRepository(context).TryAddAsync(incident, CancellationToken.None);
        return incident;
    }

    [Fact]
    public async Task TwoConcurrentAcknowledgments_OnlyOneSucceeds()
    {
        var incident = await CreateIncidentAsync();

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new SupportIncidentRepository(context1).TryAcknowledgeAsync(incident.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None),
            new SupportIncidentRepository(context2).TryAcknowledgeAsync(incident.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task ConcurrentReportsForSameSource_OnlyOneIncidentCreated()
    {
        var sourceId = Guid.NewGuid();

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var reporter1 = new SupportIncidentReporter(new SupportIncidentRepository(context1));
        var reporter2 = new SupportIncidentReporter(new SupportIncidentRepository(context2));

        var results = await Task.WhenAll(
            reporter1.ReportAsync(BuildRequest("Ride", sourceId), CancellationToken.None),
            reporter2.ReportAsync(BuildRequest("Ride", sourceId), CancellationToken.None));

        Assert.Equal(results[0], results[1]);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.SupportIncidents
            .CountAsync(i => i.SourceType == "Ride" && i.SourceId == sourceId, CancellationToken.None);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ReportsWithDifferentSourceIds_CreateSeparateIncidents()
    {
        await using var context = _fixture.CreateContext();
        var reporter = new SupportIncidentReporter(new SupportIncidentRepository(context));

        var first = await reporter.ReportAsync(BuildRequest("Ride", Guid.NewGuid()), CancellationToken.None);
        var second = await reporter.ReportAsync(BuildRequest("Ride", Guid.NewGuid()), CancellationToken.None);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task ManuallyCreatedIncidents_WithNullSource_NeverCollideOnThePartialUniqueIndex()
    {
        await using var context = _fixture.CreateContext();
        var repository = new SupportIncidentRepository(context);

        var first = SupportIncident.Create(
            SupportIncidentType.SecurityIncident, SupportIncidentSeverity.Critical, "Premier signalement manuel", "Description", null, null,
            null, null, null, DateTime.UtcNow, null, null, DateTime.UtcNow);
        var second = SupportIncident.Create(
            SupportIncidentType.SecurityIncident, SupportIncidentSeverity.Critical, "Second signalement manuel", "Description", null, null,
            null, null, null, DateTime.UtcNow, null, null, DateTime.UtcNow);

        var firstAdded = await repository.TryAddAsync(first, CancellationToken.None);
        var secondAdded = await repository.TryAddAsync(second, CancellationToken.None);

        Assert.True(firstAdded);
        Assert.True(secondAdded);
    }
}
