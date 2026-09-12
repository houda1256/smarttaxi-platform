using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>
/// TotalRevenue/PlatformCommission read the exact same fields Payments' own
/// GetRevenueSummaryQueryHandler/FinancialReportRepository already use
/// (Payment.ConfirmedAt/FinalFareAmount, FinancialLedgerEntry.EntryType==
/// PlatformCommission) — never a second, divergent definition. RevenueGrowth
/// is the one genuinely new calculation (period-over-period), following the
/// same [FromUtc, ToUtc) / previous-equal-length-period convention as every
/// other growth metric.
/// </summary>
internal sealed class FinancialAnalyticsReader : IFinancialAnalyticsReader
{
    private readonly ApplicationDbContext _context;

    public FinancialAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var totalRevenue = await GetRevenueAsync(fromUtc, toUtc, cancellationToken);
        var platformCommission = await _context.FinancialLedgerEntries
            .Where(e => e.EntryType == LedgerEntryType.PlatformCommission && e.CreatedAt >= fromUtc && e.CreatedAt < toUtc)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var previousFromUtc = fromUtc - (toUtc - fromUtc);
        var previousRevenue = await GetRevenueAsync(previousFromUtc, fromUtc, cancellationToken);

        return new FinancialAnalyticsSummary(
            fromUtc, toUtc, totalRevenue, platformCommission, GrowthMetric.Compute(totalRevenue, previousRevenue));
    }

    private async Task<decimal> GetRevenueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        await _context.Payments
            .Where(p => p.ConfirmedAt != null && p.ConfirmedAt >= fromUtc && p.ConfirmedAt < toUtc)
            .SumAsync(p => (decimal?)p.FinalFareAmount, cancellationToken) ?? 0m;
}
