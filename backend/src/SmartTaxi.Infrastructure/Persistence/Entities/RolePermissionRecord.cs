using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Infrastructure.Persistence.Entities;

internal sealed class RolePermissionRecord
{
    public UserRole Role { get; private set; }

    public string PermissionCode { get; private set; } = string.Empty;

    private RolePermissionRecord()
    {
    }

    public RolePermissionRecord(UserRole role, string permissionCode)
    {
        Role = role;
        PermissionCode = permissionCode;
    }
}
