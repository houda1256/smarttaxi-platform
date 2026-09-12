using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetSharedRideFareBreakdown;

public sealed record GetSharedRideFareBreakdownQuery(Guid MatchId) : IQuery<Result<IReadOnlyCollection<SharedRideFareShare>>>;
