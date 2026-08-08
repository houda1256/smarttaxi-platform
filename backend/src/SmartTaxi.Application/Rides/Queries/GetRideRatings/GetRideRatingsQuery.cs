using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Queries.GetRideRatings;

public sealed record GetRideRatingsQuery(Guid RideId) : IQuery<Result<IReadOnlyCollection<RideRating>>>;
