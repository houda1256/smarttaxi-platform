namespace SmartTaxi.Application.Analytics;

/// <summary>
/// Shared [FromUtc, ToUtc) validation for every Analytics query — a half-open
/// interval, `FromUtc` inclusive, `ToUtc` exclusive, capped at 366 days (a
/// full leap-year span, comfortably covering the longest spec-named cadence —
/// Quarterly — while bounding aggregation cost against the largest
/// transactional tables Ride/Payments/FinancialLedgerEntry can reach over a
/// multi-year window). Anything longer is a data-warehouse/BI concern, out of
/// this module's scope.
/// </summary>
public static class AnalyticsDateRange
{
    public const int MaxRangeDays = 366;

    public static bool IsValid(DateTime fromUtc, DateTime toUtc) =>
        fromUtc < toUtc && (toUtc - fromUtc).TotalDays <= MaxRangeDays;
}
