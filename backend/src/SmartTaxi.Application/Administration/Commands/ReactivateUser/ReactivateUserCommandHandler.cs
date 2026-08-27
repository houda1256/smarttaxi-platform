using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Administration.Commands.ReactivateUser;

/// <summary>Never restores sessions — the reactivated user must log in fresh, matching every other terminal "revoked" field in this codebase.</summary>
public sealed class ReactivateUserCommandHandler : ICommandHandler<ReactivateUserCommand, Result>
{
    private const string SelfTargetError = "Un administrateur ne peut pas se réactiver lui-même via cet endpoint.";
    private const string NotFoundError = "Utilisateur introuvable.";
    private const string AlreadyActiveError = "Cet utilisateur est déjà actif.";

    private readonly IUserRepository _userRepository;
    private readonly IAdminUserManagementRepository _repository;

    public ReactivateUserCommandHandler(IUserRepository userRepository, IAdminUserManagementRepository repository)
    {
        _userRepository = userRepository;
        _repository = repository;
    }

    public async Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetUserId == command.ActingAdminUserId)
        {
            return Result.Failure(SelfTargetError, ErrorType.Forbidden);
        }

        var target = await _userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var reactivated = await _repository.TryReactivateAsync(
            command.TargetUserId, command.ActingAdminUserId, DateTime.UtcNow, cancellationToken);

        return reactivated ? Result.Success() : Result.Failure(AlreadyActiveError, ErrorType.Conflict);
    }
}
