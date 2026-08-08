using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentTransactionHistory;

public sealed record GetPaymentTransactionHistoryQuery(Guid PaymentId) : IQuery<IReadOnlyCollection<PaymentTransactionHistory>>;
