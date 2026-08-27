using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetRideAnalytics;

public sealed record GetRideAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<RideAnalyticsSummary>>;
