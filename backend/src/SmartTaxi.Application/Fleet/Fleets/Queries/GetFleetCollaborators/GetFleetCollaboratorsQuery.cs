using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets;

namespace SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetCollaborators;

public sealed record GetFleetCollaboratorsQuery(Guid RequestingUserId, Guid FleetId)
    : IQuery<Result<IReadOnlyCollection<FleetMemberSummary>>>;
