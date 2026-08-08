using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface IRolePermissionRepository
{
    Task<IReadOnlyCollection<string>> GetPermissionsForRolesAsync(
        IReadOnlyCollection<UserRole> roles, CancellationToken cancellationToken);
}
