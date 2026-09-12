using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IAdvertisingAnalyticsReader
{
    Task<AdvertisingAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
