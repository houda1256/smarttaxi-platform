using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Commands.ExportAnalyticsReport;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Tests.Analytics.Commands;

public class ExportAnalyticsReportCommandHandlerTests
{
    private static ExportAnalyticsReportCommandHandler BuildHandler(FakeAnalyticsReportExporter exporter) => new(
        new FakeAdminDashboardReader(), new FakeGrowthAnalyticsReader(), new FakeRideAnalyticsReader(), new FakeFleetAnalyticsReader(),
        new FakeAdvertisingAnalyticsReader(), new FakeSupportAnalyticsReader(), new FakeFinancialAnalyticsReader(), exporter);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsStorageKeyAndRecordsExportMetadata()
    {
        var exporter = new FakeAnalyticsReportExporter();
        var handler = BuildHandler(exporter);
        var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(7);
        var requestedBy = Guid.NewGuid();

        var result = await handler.Handle(
            new ExportAnalyticsReportCommand(ScheduledReportCategory.Ride, fromUtc, toUtc, requestedBy, AnalyticsExportFormat.Csv),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        var call = Assert.Single(exporter.Calls);
        Assert.Equal(ScheduledReportCategory.Ride, call.Request.Category);
        Assert.Equal(requestedBy, call.Request.GeneratedBy);
        Assert.Equal(AnalyticsExportFormat.Csv, call.Format);
    }

    [Fact]
    public async Task Handle_InvalidRange_ReturnsValidationAndNeverExports()
    {
        var exporter = new FakeAnalyticsReportExporter();
        var handler = BuildHandler(exporter);
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddDays(-1);

        var result = await handler.Handle(
            new ExportAnalyticsReportCommand(ScheduledReportCategory.Ride, fromUtc, toUtc, Guid.NewGuid(), AnalyticsExportFormat.Pdf),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(exporter.Calls);
    }
}
