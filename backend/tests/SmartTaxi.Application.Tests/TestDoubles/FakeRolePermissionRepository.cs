using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRolePermissionRepository : IRolePermissionRepository
{
    private readonly Dictionary<UserRole, string[]> _permissionsByRole;

    public FakeRolePermissionRepository(Dictionary<UserRole, string[]>? permissionsByRole = null)
    {
        _permissionsByRole = permissionsByRole ?? new Dictionary<UserRole, string[]>();
    }

    public Task<IReadOnlyCollection<string>> GetPermissionsForRolesAsync(
        IReadOnlyCollection<UserRole> roles, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> permissions = roles
            .SelectMany(role => _permissionsByRole.GetValueOrDefault(role, []))
            .Distinct()
            .ToList();

        return Task.FromResult(permissions);
    }
}
