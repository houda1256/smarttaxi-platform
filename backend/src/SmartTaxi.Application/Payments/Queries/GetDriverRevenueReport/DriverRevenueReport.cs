namespace SmartTaxi.Application.Payments.Queries.GetDriverRevenueReport;

public sealed record DriverRevenueReport(Guid DriverId, decimal TotalEarned, int PaymentCount, DateTime FromDate, DateTime ToDate);
