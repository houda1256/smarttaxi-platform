using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetFleetAnalytics;

public sealed record GetFleetAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<FleetAnalyticsSummary>>;
