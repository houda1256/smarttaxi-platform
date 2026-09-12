using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>
/// Every metric is scoped by Ride.RequestedAt — the only reliable date field
/// (Ride has no dedicated CompletedAt/CancelledAt, and none is added here).
/// Direct DbContext reads, mirroring FinancialReportRepository's own
/// precedent — never a dependency on IRideRepository, which has no
/// date-range/aggregate methods.
/// </summary>
internal sealed class RideAnalyticsReader : IRideAnalyticsReader
{
    private static readonly RideStatus[] CancelledStatuses =
    [
        RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver, RideStatus.CancelledByAdmin
    ];

    private readonly ApplicationDbContext _context;

    public RideAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RideAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var ridesInPeriod = _context.Rides.Where(r => r.RequestedAt >= fromUtc && r.RequestedAt < toUtc);

        var volume = await ridesInPeriod.CountAsync(cancellationToken);
        var completedQuery = ridesInPeriod.Where(r => r.Status == RideStatus.Completed);
        var completed = await completedQuery.CountAsync(cancellationToken);
        var cancelled = await ridesInPeriod.CountAsync(r => CancelledStatuses.Contains(r.Status), cancellationToken);

        var completionRate = volume == 0 ? 0m : Math.Round((decimal)completed / volume, 4);

        var averageFare = await completedQuery.AverageAsync(r => (decimal?)r.FinalFare, cancellationToken) ?? 0m;
        var averageDistance = await completedQuery.AverageAsync(r => (decimal?)r.ActualDistanceKm, cancellationToken) ?? 0m;
        var averageDuration = await completedQuery.AverageAsync(r => (decimal?)r.ActualDurationMinutes, cancellationToken) ?? 0m;

        return new RideAnalyticsSummary(
            fromUtc, toUtc, volume, completed, cancelled, completionRate, Math.Round(averageFare, 2), Math.Round(averageDistance, 2),
            Math.Round(averageDuration, 2));
    }
}
