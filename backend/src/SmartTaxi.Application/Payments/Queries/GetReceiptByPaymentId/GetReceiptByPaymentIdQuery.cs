using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetReceiptByPaymentId;

public sealed record GetReceiptByPaymentIdQuery(Guid PaymentId) : IQuery<Result<Receipt>>;
