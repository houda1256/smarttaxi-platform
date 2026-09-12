using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Reports.Abstractions;

namespace SmartTaxi.Application.Payments.Reports.Queries.GetFinancialReport;

public sealed class GetFinancialReportQueryHandler : IQueryHandler<GetFinancialReportQuery, FinancialReportResult>
{
    private readonly IFinancialReportRepository _reportRepository;

    public GetFinancialReportQueryHandler(IFinancialReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public Task<FinancialReportResult> Handle(GetFinancialReportQuery query, CancellationToken cancellationToken) =>
        _reportRepository.GetReportAsync(query.Filter, cancellationToken);
}
