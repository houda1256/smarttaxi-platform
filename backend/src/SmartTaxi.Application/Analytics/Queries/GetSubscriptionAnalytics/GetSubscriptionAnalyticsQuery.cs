using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetSubscriptionAnalytics;

public sealed record GetSubscriptionAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<SubscriptionAnalyticsSummary>>;
