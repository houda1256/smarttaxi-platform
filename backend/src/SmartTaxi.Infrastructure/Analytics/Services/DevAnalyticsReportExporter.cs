using SmartTaxi.Application.Analytics.Abstractions;

namespace SmartTaxi.Infrastructure.Analytics.Services;

/// <summary>
/// Abstraction-only placeholder, per instructions — no PDF/Excel/CSV
/// rendering library is integrated. Returns a deterministic storage key
/// without writing any file, exactly like DevReportExporter/
/// DevInvoicePdfGenerator; a real implementation (backed by a real rendering
/// library and IFileStorageService) is a drop-in replacement behind
/// IAnalyticsReportExporter.
/// </summary>
internal sealed class DevAnalyticsReportExporter : IAnalyticsReportExporter
{
    public Task<string> ExportAsync(AnalyticsReportExportRequest request, AnalyticsExportFormat format, CancellationToken cancellationToken)
    {
        var extension = format switch
        {
            AnalyticsExportFormat.Pdf => "pdf",
            AnalyticsExportFormat.Excel => "xlsx",
            AnalyticsExportFormat.Csv => "csv",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        var key =
            $"analytics-reports/{request.Category}-{request.FromUtc:yyyyMMdd}-{request.ToUtc:yyyyMMdd}-{Guid.NewGuid():N}.{extension}";
        return Task.FromResult(key);
    }
}
