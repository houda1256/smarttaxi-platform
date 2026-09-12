namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>TotalRevenue/PlatformCommission reuse the exact same aggregation Payments already exposes — never a second, divergent computation. RevenueGrowth is the one genuinely new calculation (period-over-period), read from the same Ledger/Payment tables via the same FinancialReportRepository-style direct query precedent.</summary>
public sealed record FinancialAnalyticsSummary(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal TotalRevenue,
    decimal PlatformCommission,
    GrowthMetric RevenueGrowth);
