using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.RevokeAllSessions;

public sealed class RevokeAllSessionsCommandHandler : ICommandHandler<RevokeAllSessionsCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;

    public RevokeAllSessionsCommandHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result> Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        await _sessionRepository.RevokeAllActiveSessionsAsync(
            command.UserId, SessionRevocationReason.ManualRevocation, DateTime.UtcNow, cancellationToken);

        return Result.Success();
    }
}
