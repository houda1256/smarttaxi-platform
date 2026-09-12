using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ApplicationDbContext _context;

    public RolePermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForRolesAsync(
        IReadOnlyCollection<UserRole> roles, CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return [];
        }

        return await _context.RolePermissions
            .Where(rolePermission => roles.Contains(rolePermission.Role))
            .Select(rolePermission => rolePermission.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
