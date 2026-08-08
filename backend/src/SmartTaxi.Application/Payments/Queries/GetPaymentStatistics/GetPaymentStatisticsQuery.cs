using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentStatistics;

public sealed record GetPaymentStatisticsQuery(DateTime FromDate, DateTime ToDate) : IQuery<PaymentStatisticsReport>;
