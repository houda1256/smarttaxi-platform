using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Identity.Queries.GetUserSessions;

public sealed class GetUserSessionsQueryHandler
    : IQueryHandler<GetUserSessionsQuery, Result<IReadOnlyCollection<SessionSummaryResult>>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetUserSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<IReadOnlyCollection<SessionSummaryResult>>> Handle(
        GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(query.UserId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var results = sessions
            .Select(session => new SessionSummaryResult(
                session.Id,
                session.CreatedAt,
                session.LastActivityAt,
                session.ExpiresAt,
                session.DeviceLabel,
                session.IsActive(utcNow)))
            .ToList();

        return Result<IReadOnlyCollection<SessionSummaryResult>>.Success(results);
    }
}
