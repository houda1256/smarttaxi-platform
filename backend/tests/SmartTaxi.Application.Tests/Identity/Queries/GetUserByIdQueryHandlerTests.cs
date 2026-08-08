using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Queries.GetUserById;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Queries;

public class GetUserByIdQueryHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsRolesAndPermissions()
    {
        var user = User.Create(Email.Create("admin@example.com"), HashedPassword.Create("hash"), UserRole.Admin);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var rolePermissionRepository = new FakeRolePermissionRepository(new Dictionary<UserRole, string[]>
        {
            [UserRole.Admin] = ["users.read", "users.manage"]
        });

        var handler = new GetUserByIdQueryHandler(_userRepository, rolePermissionRepository);

        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.UserId);
        Assert.Equal("admin@example.com", result.Value.Email);
        Assert.Contains(nameof(UserRole.Admin), result.Value.Roles);
        Assert.Contains("users.read", result.Value.Permissions);
        Assert.Contains("users.manage", result.Value.Permissions);
    }

    [Fact]
    public async Task Handle_WithUnknownUserId_ReturnsNotFoundFailure()
    {
        var handler = new GetUserByIdQueryHandler(_userRepository, new FakeRolePermissionRepository());

        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
