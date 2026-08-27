using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IFinancialAnalyticsReader
{
    Task<FinancialAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
