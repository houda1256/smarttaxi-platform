using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Reports.Queries.GetFinancialReport;

public sealed record GetFinancialReportQuery(FinancialReportFilter Filter) : IQuery<FinancialReportResult>;
