using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IFleetAnalyticsReader
{
    Task<FleetAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
