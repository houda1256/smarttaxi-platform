namespace SmartTaxi.Application.Payments.Queries.GetRevenueSummary;

public sealed record RevenuePeriodSummary(
    DateTime PeriodStart, decimal TotalRevenue, decimal PlatformCommission, decimal DriverPayouts, decimal OwnerPayouts,
    decimal RefundedAmount, int PaymentCount);
