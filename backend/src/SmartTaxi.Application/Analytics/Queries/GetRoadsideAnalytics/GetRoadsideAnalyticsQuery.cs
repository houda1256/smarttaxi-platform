using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetRoadsideAnalytics;

public sealed record GetRoadsideAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<RoadsideAnalyticsSummary>>;
