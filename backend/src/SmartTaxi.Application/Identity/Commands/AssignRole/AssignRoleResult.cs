namespace SmartTaxi.Application.Identity.Commands.AssignRole;

public sealed record AssignRoleResult(Guid UserId, IReadOnlyCollection<string> Roles);
