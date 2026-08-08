using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentByIdAdmin;

public sealed record GetPaymentByIdAdminQuery(Guid PaymentId) : IQuery<Result<PaymentSummary>>;
