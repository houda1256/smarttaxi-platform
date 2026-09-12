using SmartTaxi.Application.Payments.Reports;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record FinancialReportResponse(
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
    decimal TaxTotal)
{
    public static FinancialReportResponse FromResult(FinancialReportResult result) => new(
        result.GrossRevenue, result.NetRevenue, result.PlatformCommission, result.DriverEarnings, result.OwnerEarnings,
        result.PartnerEarnings, result.RefundAmount, result.OutstandingInvoices, result.CashCollected, result.CardPayments,
        result.Expenses, result.NetProfit, result.PayoutsTotal, result.DebtsTotal, result.CashDifferencesTotal, result.TaxTotal);
}
