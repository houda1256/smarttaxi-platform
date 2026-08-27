using SmartTaxi.Application.Administration.Commands.ReactivateUser;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Administration.Commands;

public class ReactivateUserCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeAdminUserManagementRepository _repository = new();
    private readonly ReactivateUserCommandHandler _handler;

    public ReactivateUserCommandHandlerTests()
    {
        _handler = new ReactivateUserCommandHandler(_userRepository, _repository);
    }

    private async Task<Guid> SeedSuspendedUserAsync()
    {
        var user = User.Create(Email.Create("target@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        _repository.SeedUser(user.Id, isActive: false);
        return user.Id;
    }

    [Fact]
    public async Task Handle_WithSelfTarget_ReturnsForbidden()
    {
        var adminId = await SeedSuspendedUserAsync();

        var result = await _handler.Handle(new ReactivateUserCommand(adminId, adminId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownTarget_ReturnsNotFound()
    {
        var result = await _handler.Handle(new ReactivateUserCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithSuspendedTarget_Succeeds()
    {
        var targetId = await SeedSuspendedUserAsync();

        var result = await _handler.Handle(new ReactivateUserCommand(targetId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ReturnsConflict()
    {
        var targetId = await SeedSuspendedUserAsync();
        var actorId = Guid.NewGuid();
        await _handler.Handle(new ReactivateUserCommand(targetId, actorId), CancellationToken.None);

        var result = await _handler.Handle(new ReactivateUserCommand(targetId, actorId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
