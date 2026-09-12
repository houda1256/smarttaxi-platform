using SmartTaxi.Application.Analytics.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAnalyticsReportExporter : IAnalyticsReportExporter
{
    public List<(AnalyticsReportExportRequest Request, AnalyticsExportFormat Format)> Calls { get; } = [];

    public Task<string> ExportAsync(AnalyticsReportExportRequest request, AnalyticsExportFormat format, CancellationToken cancellationToken)
    {
        Calls.Add((request, format));
        return Task.FromResult($"analytics-reports/{request.Category}-{Guid.NewGuid():N}.{format}");
    }
}
