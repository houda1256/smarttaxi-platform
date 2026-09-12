using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetDriverRevenueReport;

public sealed record GetDriverRevenueReportQuery(Guid DriverId, DateTime FromDate, DateTime ToDate) : IQuery<DriverRevenueReport>;
