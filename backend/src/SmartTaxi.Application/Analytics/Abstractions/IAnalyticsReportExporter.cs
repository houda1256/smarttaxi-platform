using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Analytics.Abstractions;

public enum AnalyticsExportFormat
{
    Pdf,
    Excel,
    Csv
}

/// <summary>
/// Analytics' own export seam — deliberately separate from Payments'
/// IReportExporter (which is hard-typed to FinancialReportFilter/
/// FinancialReportResult) so financial exports stay exclusively Payments-owned
/// and are never duplicated here. Abstraction only, per instructions: no real
/// PDF/Excel/CSV rendering library is integrated — the Infrastructure
/// implementation is a documented dev stub, exactly like
/// DevReportExporter/DevInvoicePdfGenerator.
/// </summary>
public interface IAnalyticsReportExporter
{
    Task<string> ExportAsync(AnalyticsReportExportRequest request, AnalyticsExportFormat format, CancellationToken cancellationToken);
}

/// <summary>
/// The export-metadata fields the spec names (line 333): GeneratedBy/
/// GeneratedAt/Filters/RecordCount/ExportId/ConfidentialityNotice. Never
/// persisted — computed fresh for each export call, same as every other
/// report in this codebase.
/// </summary>
public sealed record AnalyticsReportExportRequest(
    ScheduledReportCategory Category,
    DateTime FromUtc,
    DateTime ToUtc,
    Guid GeneratedBy,
    DateTime GeneratedAt,
    int RecordCount,
    string ConfidentialityNotice);
