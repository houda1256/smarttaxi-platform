namespace SmartTaxi.API.Contracts.Identity;

public sealed record AssignRoleResponse(Guid UserId, IReadOnlyCollection<string> Roles);
