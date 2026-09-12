namespace SmartTaxi.Application.Payments.Queries.GetPaymentStatistics;

public sealed record PaymentStatisticsReport(
    int TotalPayments,
    int PendingCount,
    int AuthorizedCount,
    int PaidCount,
    int FailedCount,
    int CancelledCount,
    int RefundedCount,
    int PartiallyRefundedCount,
    decimal TotalRevenue,
    decimal TotalRefunded,
    decimal AverageFare,
    DateTime FromDate,
    DateTime ToDate);
