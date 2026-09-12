using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery(Guid UserId) : IQuery<Result<IReadOnlyCollection<SessionSummaryResult>>>;
