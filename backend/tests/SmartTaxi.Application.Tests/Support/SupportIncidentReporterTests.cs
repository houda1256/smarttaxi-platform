using SmartTaxi.Application.Support;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support;

public class SupportIncidentReporterTests
{
    private readonly FakeSupportIncidentRepository _incidentRepository = new();
    private readonly SupportIncidentReporter _reporter;

    public SupportIncidentReporterTests()
    {
        _reporter = new SupportIncidentReporter(_incidentRepository);
    }

    private static SupportIncidentReportRequest BuildRequest(string? sourceType, Guid? sourceId) => new(
        SupportIncidentType.RideIncident, SupportIncidentSeverity.Moderate, "Titre", "Description", Guid.NewGuid(), null, null, null, null,
        DateTime.UtcNow, sourceType, sourceId);

    [Fact]
    public async Task ReportAsync_WithoutSource_AlwaysCreatesNewIncident()
    {
        var first = await _reporter.ReportAsync(BuildRequest(null, null), CancellationToken.None);
        var second = await _reporter.ReportAsync(BuildRequest(null, null), CancellationToken.None);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task ReportAsync_WithSameSource_IsIdempotent()
    {
        var sourceId = Guid.NewGuid();

        var first = await _reporter.ReportAsync(BuildRequest("Ride", sourceId), CancellationToken.None);
        var second = await _reporter.ReportAsync(BuildRequest("Ride", sourceId), CancellationToken.None);

        Assert.Equal(first, second);
        var all = await _incidentRepository.GetAllAsync(1, 10, CancellationToken.None);
        Assert.Single(all.Items);
    }

    [Fact]
    public async Task ReportAsync_WithDifferentSourceIds_CreatesSeparateIncidents()
    {
        var first = await _reporter.ReportAsync(BuildRequest("Ride", Guid.NewGuid()), CancellationToken.None);
        var second = await _reporter.ReportAsync(BuildRequest("Ride", Guid.NewGuid()), CancellationToken.None);

        Assert.NotEqual(first, second);
    }
}
