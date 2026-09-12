using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(Guid RequestingUserId, Guid PaymentId) : IQuery<Result<PaymentSummary>>;
