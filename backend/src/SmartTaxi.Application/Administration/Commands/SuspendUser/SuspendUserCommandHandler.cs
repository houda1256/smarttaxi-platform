using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Administration.Commands.SuspendUser;

/// <summary>Admin endpoints administer ANOTHER account — self-targeting is rejected before any repository call, never treated as an alternative self-service deactivation path.</summary>
public sealed class SuspendUserCommandHandler : ICommandHandler<SuspendUserCommand, Result>
{
    private const string SelfTargetError = "Un administrateur ne peut pas se suspendre lui-même.";
    private const string NotFoundError = "Utilisateur introuvable.";
    private const string AlreadySuspendedError = "Cet utilisateur est déjà suspendu.";

    private readonly IUserRepository _userRepository;
    private readonly IAdminUserManagementRepository _repository;

    public SuspendUserCommandHandler(IUserRepository userRepository, IAdminUserManagementRepository repository)
    {
        _userRepository = userRepository;
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendUserCommand command, CancellationToken cancellationToken)
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

        var suspended = await _repository.TrySuspendAsync(
            command.TargetUserId, command.ActingAdminUserId, DateTime.UtcNow, cancellationToken);

        return suspended ? Result.Success() : Result.Failure(AlreadySuspendedError, ErrorType.Conflict);
    }
}
