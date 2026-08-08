using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Application.Payments.Queries.GetInvoiceByPaymentId;

public sealed record GetInvoiceByPaymentIdQuery(Guid PaymentId) : IQuery<Result<Invoice>>;
