using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Identity.Entities;

public sealed class UserRoleAssignment
{
    public Guid UserId { get; private set; }

    public UserRole Role { get; private set; }

    private UserRoleAssignment()
    {
    }

    internal UserRoleAssignment(Guid userId, UserRole role)
    {
        UserId = userId;
        Role = role;
    }
}
