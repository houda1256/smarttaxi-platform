using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetRecommendedDrivers;

public sealed record GetRecommendedDriversQuery(Guid RequestingUserId, Guid RideId)
    : IQuery<Result<IReadOnlyCollection<RideDriverRecommendationSummary>>>;
