using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetMyPaymentsForCustomer;

public sealed record GetMyPaymentsForCustomerQuery(Guid CustomerId) : IQuery<IReadOnlyCollection<PaymentSummary>>;
