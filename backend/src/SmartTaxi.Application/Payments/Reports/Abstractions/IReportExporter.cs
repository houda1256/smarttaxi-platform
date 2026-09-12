namespace SmartTaxi.Application.Payments.Reports.Abstractions;

public enum ReportExportFormat
{
    Pdf,
    Excel,
    Csv
}

/// <summary>Abstraction only, per instructions — no real PDF/Excel/CSV rendering library is integrated; the Infrastructure implementation is a documented dev stub, exactly like IInvoicePdfGenerator/IReceiptPdfGenerator.</summary>
public interface IReportExporter
{
    Task<string> ExportAsync(FinancialReportFilter filter, FinancialReportResult report, ReportExportFormat format, CancellationToken cancellationToken);
}
