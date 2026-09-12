using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IRideAnalyticsReader
{
    Task<RideAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
