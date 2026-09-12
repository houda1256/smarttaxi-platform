using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetSupportAnalytics;

public sealed record GetSupportAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<SupportAnalyticsSummary>>;
