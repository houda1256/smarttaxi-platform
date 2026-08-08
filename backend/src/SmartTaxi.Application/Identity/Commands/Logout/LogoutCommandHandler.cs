using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;

    public LogoutCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);

        // Nothing to revoke is treated as already-logged-out (idempotent), not
        // an error — the session id always comes from the caller's own token.
        if (session is null)
        {
            return Result.Success();
        }

        session.Revoke(SessionRevocationReason.LoggedOut, DateTime.UtcNow);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return Result.Success();
    }
}
