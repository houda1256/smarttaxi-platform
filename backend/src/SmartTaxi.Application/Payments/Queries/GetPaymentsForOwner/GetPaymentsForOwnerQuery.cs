using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentsForOwner;

public sealed record GetPaymentsForOwnerQuery(Guid OwnerId) : IQuery<IReadOnlyCollection<PaymentSummary>>;
