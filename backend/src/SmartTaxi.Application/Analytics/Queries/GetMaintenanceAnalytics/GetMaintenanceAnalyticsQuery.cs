using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetMaintenanceAnalytics;

public sealed record GetMaintenanceAnalyticsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<Result<MaintenanceAnalyticsSummary>>;
