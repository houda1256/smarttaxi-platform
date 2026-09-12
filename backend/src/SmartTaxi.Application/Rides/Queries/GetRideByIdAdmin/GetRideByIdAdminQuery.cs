using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetRideByIdAdmin;

public sealed record GetRideByIdAdminQuery(Guid RideId) : IQuery<Result<RideSummary>>;
