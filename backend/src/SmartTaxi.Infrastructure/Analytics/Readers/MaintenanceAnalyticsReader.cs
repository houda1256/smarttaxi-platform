using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

internal sealed class MaintenanceAnalyticsReader : IMaintenanceAnalyticsReader
{
    private readonly ApplicationDbContext _context;

    public MaintenanceAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var requestsInPeriod = _context.MaintenanceRequests.Where(r => r.RequestedAtUtc >= fromUtc && r.RequestedAtUtc < toUtc);

        var requestCount = await requestsInPeriod.CountAsync(cancellationToken);
        var completedQuery = requestsInPeriod.Where(r => r.Status == MaintenanceRequestStatus.Completed);
        var completedCount = await completedQuery.CountAsync(cancellationToken);
        var completionRate = requestCount == 0 ? 0m : Math.Round((decimal)completedCount / requestCount, 4);

        var averageFinalCost = await completedQuery.AverageAsync(r => (decimal?)r.FinalCost, cancellationToken) ?? 0m;

        // Averaged client-side over a bounded, period-scoped, already-completed set — avoids relying on
        // provider-specific DateTime-subtraction translation for a single small aggregate.
        var turnarounds = await completedQuery
            .Where(r => r.CompletedAtUtc != null)
            .Select(r => new { r.RequestedAtUtc, CompletedAtUtc = r.CompletedAtUtc!.Value })
            .ToListAsync(cancellationToken);

        var averageTurnaroundHours = turnarounds.Count == 0
            ? 0m
            : (decimal)turnarounds.Average(t => (t.CompletedAtUtc - t.RequestedAtUtc).TotalHours);

        return new MaintenanceAnalyticsSummary(
            fromUtc, toUtc, requestCount, completedCount, completionRate, Math.Round(averageFinalCost, 2),
            Math.Round(averageTurnaroundHours, 2));
    }
}
