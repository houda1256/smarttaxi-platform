using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Domain.Identity.Entities;

public sealed class User : AggregateRoot
{
    public Email Email { get; private set; }
    public HashedPassword PasswordHash { get; private set; }
    public UserRole Role { get; private set; }

    private User(Guid id, Email email, HashedPassword passwordHash, UserRole role)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(Email email, HashedPassword passwordHash, UserRole role)
    {
        return new User(Guid.NewGuid(), email, passwordHash, role);
    }
}
