using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler : ICommandHandler<RemoveRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public RemoveRoleCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(RemoveRoleCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
        {
            return Result.Failure($"Le rôle '{command.Role}' est invalide.", ErrorType.Validation);
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure("Utilisateur introuvable.", ErrorType.NotFound);
        }

        if (!user.CanRemoveRole(role))
        {
            return Result.Failure("Impossible de retirer le dernier rôle de l'utilisateur.", ErrorType.Validation);
        }

        user.RemoveRole(role);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}
