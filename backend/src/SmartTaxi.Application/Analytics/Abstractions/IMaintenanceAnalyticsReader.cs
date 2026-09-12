using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IMaintenanceAnalyticsReader
{
    Task<MaintenanceAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
