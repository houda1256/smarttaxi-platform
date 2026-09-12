using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetOwnerRevenueReport;

public sealed record GetOwnerRevenueReportQuery(Guid OwnerId, DateTime FromDate, DateTime ToDate) : IQuery<OwnerRevenueReport>;
