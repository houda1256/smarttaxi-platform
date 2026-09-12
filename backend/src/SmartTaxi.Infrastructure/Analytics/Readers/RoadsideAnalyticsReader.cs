using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>No GPS, no ETA, no route analytics — Latitude/Longitude are never selected here.</summary>
internal sealed class RoadsideAnalyticsReader : IRoadsideAnalyticsReader
{
    private readonly ApplicationDbContext _context;

    public RoadsideAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoadsideAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var requestsInPeriod = _context.RoadsideAssistanceRequests.Where(r => r.RequestedAtUtc >= fromUtc && r.RequestedAtUtc < toUtc);

        var requestCount = await requestsInPeriod.CountAsync(cancellationToken);
        var completedQuery = requestsInPeriod.Where(r => r.Status == RoadsideRequestStatus.Completed);
        var completedCount = await completedQuery.CountAsync(cancellationToken);
        var completionRate = requestCount == 0 ? 0m : Math.Round((decimal)completedCount / requestCount, 4);

        var averageFinalCost = await completedQuery.AverageAsync(r => (decimal?)r.FinalCost, cancellationToken) ?? 0m;

        var durations = await completedQuery
            .Where(r => r.StartedAtUtc != null && r.CompletedAtUtc != null)
            .Select(r => new { StartedAtUtc = r.StartedAtUtc!.Value, CompletedAtUtc = r.CompletedAtUtc!.Value })
            .ToListAsync(cancellationToken);

        var averageDurationMinutes = durations.Count == 0
            ? 0m
            : (decimal)durations.Average(d => (d.CompletedAtUtc - d.StartedAtUtc).TotalMinutes);

        return new RoadsideAnalyticsSummary(
            fromUtc, toUtc, requestCount, completedCount, completionRate, Math.Round(averageFinalCost, 2),
            Math.Round(averageDurationMinutes, 2));
    }
}
