using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetMyRidesForCustomer;

public sealed record GetMyRidesForCustomerQuery(Guid CustomerId) : IQuery<IReadOnlyCollection<RideSummary>>;
