using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetRideById;

public sealed record GetRideByIdQuery(Guid RequestingUserId, Guid RideId) : IQuery<Result<RideSummary>>;
