using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.AssignRole;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class AssignRoleCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeAuditedUserRepository _auditedUserRepository = new();
    private readonly AssignRoleCommandHandler _handler;
    private readonly Guid _actingAdminUserId = Guid.NewGuid();

    public AssignRoleCommandHandlerTests()
    {
        _handler = new AssignRoleCommandHandler(_userRepository, _auditedUserRepository, new FakeAuditContextAccessor());
    }

    private async Task<User> SeedUserAsync()
    {
        var user = User.Create(Email.Create("user@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidRole_AssignsItAndPersists()
    {
        var user = await SeedUserAsync();

        var result = await _handler.Handle(new AssignRoleCommand(user.Id, nameof(UserRole.Driver), _actingAdminUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(nameof(UserRole.Driver), result.Value!.Roles);
        Assert.True(user.HasRole(UserRole.Driver));

        var auditEntry = Assert.Single(_auditedUserRepository.AuditEntries);
        Assert.Equal(SmartTaxi.Domain.Administration.Enums.AuditAction.RoleAssigned, auditEntry.Action);
        Assert.Equal(_actingAdminUserId, auditEntry.ActorUserId);
        Assert.Equal(user.Id, auditEntry.TargetId);
        Assert.Equal($$"""{"role":"{{nameof(UserRole.Driver)}}"}""", auditEntry.Metadata);
    }

    [Fact]
    public async Task Handle_WithUnknownRoleName_ReturnsValidationFailure()
    {
        var user = await SeedUserAsync();

        var result = await _handler.Handle(new AssignRoleCommand(user.Id, "NotARole", _actingAdminUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownUserId_ReturnsNotFoundFailure()
    {
        var result = await _handler.Handle(new AssignRoleCommand(Guid.NewGuid(), nameof(UserRole.Driver), _actingAdminUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
