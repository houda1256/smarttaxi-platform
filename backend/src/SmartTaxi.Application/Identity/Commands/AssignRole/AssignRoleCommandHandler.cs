using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.AssignRole;

public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, Result<AssignRoleResult>>
{
    private readonly IUserRepository _userRepository;

    public AssignRoleCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<AssignRoleResult>> Handle(AssignRoleCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
        {
            return Result<AssignRoleResult>.Failure($"Le rôle '{command.Role}' est invalide.", ErrorType.Validation);
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AssignRoleResult>.Failure("Utilisateur introuvable.", ErrorType.NotFound);
        }

        user.AssignRole(role);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<AssignRoleResult>.Success(
            new AssignRoleResult(user.Id, user.Roles.Select(r => r.ToString()).ToList()));
    }
}
