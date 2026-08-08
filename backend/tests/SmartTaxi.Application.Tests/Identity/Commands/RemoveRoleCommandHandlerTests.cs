using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.RemoveRole;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RemoveRoleCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly RemoveRoleCommandHandler _handler;

    public RemoveRoleCommandHandlerTests()
    {
        _handler = new RemoveRoleCommandHandler(_userRepository);
    }

    private async Task<User> SeedUserAsync(params UserRole[] extraRoles)
    {
        var user = User.Create(Email.Create("user@example.com"), HashedPassword.Create("hash"), UserRole.Customer);
        foreach (var role in extraRoles)
        {
            user.AssignRole(role);
        }

        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_WhenUserHasMoreThanOneRole_RemovesIt()
    {
        var user = await SeedUserAsync(UserRole.Driver);

        var result = await _handler.Handle(new RemoveRoleCommand(user.Id, nameof(UserRole.Driver)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.HasRole(UserRole.Driver));
    }

    [Fact]
    public async Task Handle_WhenItWouldLeaveZeroRoles_ReturnsValidationFailure()
    {
        var user = await SeedUserAsync();

        var result = await _handler.Handle(new RemoveRoleCommand(user.Id, nameof(UserRole.Customer)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.True(user.HasRole(UserRole.Customer));
    }

    [Fact]
    public async Task Handle_WithUnknownRoleName_ReturnsValidationFailure()
    {
        var user = await SeedUserAsync();

        var result = await _handler.Handle(new RemoveRoleCommand(user.Id, "NotARole"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownUserId_ReturnsNotFoundFailure()
    {
        var result = await _handler.Handle(new RemoveRoleCommand(Guid.NewGuid(), nameof(UserRole.Customer)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
