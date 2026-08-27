using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetAdvertisingAnalytics;

public sealed record GetAdvertisingAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<AdvertisingAnalyticsSummary>>;
