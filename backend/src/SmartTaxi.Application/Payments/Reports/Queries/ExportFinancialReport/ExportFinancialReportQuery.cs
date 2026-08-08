using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Reports.Abstractions;

namespace SmartTaxi.Application.Payments.Reports.Queries.ExportFinancialReport;

public sealed record ExportFinancialReportQuery(FinancialReportFilter Filter, ReportExportFormat Format) : IQuery<string>;
