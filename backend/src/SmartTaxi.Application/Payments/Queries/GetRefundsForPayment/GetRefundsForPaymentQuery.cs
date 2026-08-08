using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetRefundsForPayment;

public sealed record GetRefundsForPaymentQuery(Guid PaymentId) : IQuery<IReadOnlyCollection<RefundRecord>>;
