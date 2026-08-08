using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets;

namespace SmartTaxi.Application.Fleet.Fleets.Queries.GetMyFleets;

public sealed record GetMyFleetsQuery(Guid OwnerId) : IQuery<IReadOnlyCollection<FleetSummary>>;
