using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.AssignRole;

/// <summary>
/// All existing authorization/validation/business semantics are unchanged —
/// the only thing modified from the pre-Module-13A version is the final
/// persistence call, which now also atomically writes a RoleAssigned audit
/// entry (IAuditedUserRepository.SaveWithAuditAsync) instead of a plain
/// IUserRepository.UpdateAsync.
/// </summary>
public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, Result<AssignRoleResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditedUserRepository _auditedUserRepository;
    private readonly IAuditContextAccessor _auditContextAccessor;

    public AssignRoleCommandHandler(
        IUserRepository userRepository, IAuditedUserRepository auditedUserRepository, IAuditContextAccessor auditContextAccessor)
    {
        _userRepository = userRepository;
        _auditedUserRepository = auditedUserRepository;
        _auditContextAccessor = auditContextAccessor;
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

        var context = _auditContextAccessor.GetContext();
        var auditEntry = AuditLogEntry.Create(
            command.ActingAdminUserId, AuditAction.RoleAssigned, AuditTargetType.User, user.Id,
            new Dictionary<string, string> { ["role"] = role.ToString() },
            context.CorrelationId, context.IpAddress, context.UserAgent, DateTime.UtcNow);

        await _auditedUserRepository.SaveWithAuditAsync(user, auditEntry, cancellationToken);

        return Result<AssignRoleResult>.Success(
            new AssignRoleResult(user.Id, user.Roles.Select(r => r.ToString()).ToList()));
    }
}
