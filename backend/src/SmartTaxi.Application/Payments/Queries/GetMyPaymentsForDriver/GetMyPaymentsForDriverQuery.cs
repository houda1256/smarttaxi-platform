using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Queries.GetMyPaymentsForDriver;

public sealed record GetMyPaymentsForDriverQuery(Guid DriverProfileId) : IQuery<IReadOnlyCollection<PaymentSummary>>;
