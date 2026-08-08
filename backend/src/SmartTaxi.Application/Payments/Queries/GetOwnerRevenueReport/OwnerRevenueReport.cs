namespace SmartTaxi.Application.Payments.Queries.GetOwnerRevenueReport;

public sealed record OwnerRevenueReport(Guid OwnerId, decimal TotalEarned, int PaymentCount, DateTime FromDate, DateTime ToDate);
