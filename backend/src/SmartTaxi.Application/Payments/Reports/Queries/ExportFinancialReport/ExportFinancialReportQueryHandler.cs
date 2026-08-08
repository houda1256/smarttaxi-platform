using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Reports.Abstractions;

namespace SmartTaxi.Application.Payments.Reports.Queries.ExportFinancialReport;

public sealed class ExportFinancialReportQueryHandler : IQueryHandler<ExportFinancialReportQuery, string>
{
    private readonly IFinancialReportRepository _reportRepository;
    private readonly IReportExporter _exporter;

    public ExportFinancialReportQueryHandler(IFinancialReportRepository reportRepository, IReportExporter exporter)
    {
        _reportRepository = reportRepository;
        _exporter = exporter;
    }

    public async Task<string> Handle(ExportFinancialReportQuery query, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetReportAsync(query.Filter, cancellationToken);
        return await _exporter.ExportAsync(query.Filter, report, query.Format, cancellationToken);
    }
}
