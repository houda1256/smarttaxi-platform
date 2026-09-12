using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetFinancialAnalytics;

public sealed record GetFinancialAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<FinancialAnalyticsSummary>>;
