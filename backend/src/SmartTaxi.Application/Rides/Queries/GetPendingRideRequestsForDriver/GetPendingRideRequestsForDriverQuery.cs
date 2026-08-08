using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetPendingRideRequestsForDriver;

public sealed record GetPendingRideRequestsForDriverQuery(Guid DriverUserId) : IQuery<IReadOnlyCollection<RideSummary>>;
