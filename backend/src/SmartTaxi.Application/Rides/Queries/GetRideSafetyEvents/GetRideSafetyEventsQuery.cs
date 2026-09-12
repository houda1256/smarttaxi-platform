using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Queries.GetRideSafetyEvents;

public sealed record GetRideSafetyEventsQuery(Guid RideId) : IQuery<Result<IReadOnlyCollection<RideSafetyEvent>>>;
