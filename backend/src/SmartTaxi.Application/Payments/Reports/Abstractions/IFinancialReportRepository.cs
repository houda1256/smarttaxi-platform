namespace SmartTaxi.Application.Payments.Reports.Abstractions;

public interface IFinancialReportRepository
{
    Task<FinancialReportResult> GetReportAsync(FinancialReportFilter filter, CancellationToken cancellationToken);
}
