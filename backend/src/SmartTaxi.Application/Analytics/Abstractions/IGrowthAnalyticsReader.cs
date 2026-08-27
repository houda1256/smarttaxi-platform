using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IGrowthAnalyticsReader
{
    Task<GrowthAnalyticsResult> GetGrowthAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
