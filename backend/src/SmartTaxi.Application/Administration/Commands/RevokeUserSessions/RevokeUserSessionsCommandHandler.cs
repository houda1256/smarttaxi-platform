using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Administration.Commands.RevokeUserSessions;

/// <summary>Idempotent by construction — repeated calls always succeed and always audit, even when zero sessions were left to revoke.</summary>
public sealed class RevokeUserSessionsCommandHandler : ICommandHandler<RevokeUserSessionsCommand, Result<int>>
{
    private const string SelfTargetError = "Un administrateur ne peut pas révoquer ses propres sessions via cet endpoint.";
    private const string NotFoundError = "Utilisateur introuvable.";

    private readonly IAdminUserManagementRepository _repository;

    public RevokeUserSessionsCommandHandler(IAdminUserManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(RevokeUserSessionsCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetUserId == command.ActingAdminUserId)
        {
            return Result<int>.Failure(SelfTargetError, ErrorType.Forbidden);
        }

        var revokedCount = await _repository.TryRevokeSessionsAsync(
            command.TargetUserId, command.ActingAdminUserId, DateTime.UtcNow, cancellationToken);

        return revokedCount is null ? Result<int>.Failure(NotFoundError, ErrorType.NotFound) : Result<int>.Success(revokedCount.Value);
    }
}
