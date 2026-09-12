using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Queries.GetAdminRides;

public sealed record GetAdminRidesQuery(RideStatus? Status) : IQuery<IReadOnlyCollection<RideSummary>>;
