using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler : ICommandHandler<RevokeSessionCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;

    public RevokeSessionCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);

        // A session that doesn't exist AND one that belongs to someone else both
        // return the same NotFound outcome — never confirm another user's session id.
        if (session is null || session.UserId != command.UserId)
        {
            return Result.Failure("Session introuvable.", ErrorType.NotFound);
        }

        session.Revoke(SessionRevocationReason.ManualRevocation, DateTime.UtcNow);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return Result.Success();
    }
}
