using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetGrowthAnalytics;

public sealed record GetGrowthAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<GrowthAnalyticsResult>>;
