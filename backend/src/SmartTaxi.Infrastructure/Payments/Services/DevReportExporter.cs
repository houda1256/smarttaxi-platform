using SmartTaxi.Application.Payments.Reports;
using SmartTaxi.Application.Payments.Reports.Abstractions;

namespace SmartTaxi.Infrastructure.Payments.Services;

/// <summary>
/// Abstraction-only placeholder, per instructions — no PDF/Excel/CSV
/// rendering library is integrated. Returns a deterministic storage key
/// without writing any file, exactly like DevInvoicePdfGenerator/
/// DevReceiptPdfGenerator; a real implementation (QuestPDF/ClosedXML/CsvHelper
/// -backed, persisted via IFileStorageService) is a drop-in replacement
/// behind IReportExporter.
/// </summary>
internal sealed class DevReportExporter : IReportExporter
{
    public Task<string> ExportAsync(FinancialReportFilter filter, FinancialReportResult report, ReportExportFormat format, CancellationToken cancellationToken)
    {
        var extension = format switch
        {
            ReportExportFormat.Pdf => "pdf",
            ReportExportFormat.Excel => "xlsx",
            ReportExportFormat.Csv => "csv",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        var key = $"reports/financial-{filter.FromUtc:yyyyMMdd}-{filter.ToUtc:yyyyMMdd}-{Guid.NewGuid():N}.{extension}";
        return Task.FromResult(key);
    }
}
