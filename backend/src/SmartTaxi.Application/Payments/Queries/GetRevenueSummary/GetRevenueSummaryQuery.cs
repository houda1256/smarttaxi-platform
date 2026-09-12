using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetRevenueSummary;

/// <summary>Daily or monthly revenue, driven by a single Granularity parameter rather than two near-duplicate queries.</summary>
public sealed record GetRevenueSummaryQuery(DateTime FromDate, DateTime ToDate, RevenueReportGranularity Granularity)
    : IQuery<IReadOnlyCollection<RevenuePeriodSummary>>;
