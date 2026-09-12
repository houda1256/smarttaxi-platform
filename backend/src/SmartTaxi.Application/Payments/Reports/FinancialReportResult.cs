namespace SmartTaxi.Application.Payments.Reports;

/// <summary>One row per report line the master prompt names — all computed from existing tables, nothing stored redundantly.</summary>
public sealed record FinancialReportResult(
    decimal GrossRevenue,
    decimal NetRevenue,
    decimal PlatformCommission,
    decimal DriverEarnings,
    decimal OwnerEarnings,
    decimal PartnerEarnings,
    decimal RefundAmount,
    decimal OutstandingInvoices,
    decimal CashCollected,
    decimal CardPayments,
    decimal Expenses,
    decimal NetProfit,
    decimal PayoutsTotal,
    decimal DebtsTotal,
    decimal CashDifferencesTotal,
    decimal TaxTotal);
