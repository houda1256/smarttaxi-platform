using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets;

namespace SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetById;

public sealed record GetFleetByIdQuery(Guid RequestingUserId, Guid FleetId) : IQuery<Result<FleetSummary>>;
