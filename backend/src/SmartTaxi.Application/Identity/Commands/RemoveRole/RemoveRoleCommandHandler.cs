using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.RemoveRole;

/// <summary>All existing authorization/validation/business semantics are unchanged — only the final persistence call now also atomically writes a RoleRemoved audit entry.</summary>
public sealed class RemoveRoleCommandHandler : ICommandHandler<RemoveRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditedUserRepository _auditedUserRepository;

    public RemoveRoleCommandHandler(IUserRepository userRepository, IAuditedUserRepository auditedUserRepository)
    {
        _userRepository = userRepository;
        _auditedUserRepository = auditedUserRepository;
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

        var auditEntry = AuditLogEntry.Create(
            command.ActingAdminUserId, AuditAction.RoleRemoved, AuditTargetType.User, user.Id,
            new Dictionary<string, string> { ["role"] = role.ToString() }, null, null, null, DateTime.UtcNow);

        await _auditedUserRepository.SaveWithAuditAsync(user, auditEntry, cancellationToken);

        return Result.Success();
    }
}
