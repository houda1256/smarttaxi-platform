using SmartTaxi.Application.Administration.Commands.SuspendUser;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Administration.Commands;

public class SuspendUserCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeAdminUserManagementRepository _repository = new();
    private readonly SuspendUserCommandHandler _handler;

    public SuspendUserCommandHandlerTests()
    {
        _handler = new SuspendUserCommandHandler(_userRepository, _repository);
    }

    private async Task<Guid> SeedActiveUserAsync()
    {
        var user = User.Create(Email.Create("target@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        _repository.SeedUser(user.Id, isActive: true);
        return user.Id;
    }

    [Fact]
    public async Task Handle_WithSelfTarget_ReturnsForbidden()
    {
        var adminId = await SeedActiveUserAsync();

        var result = await _handler.Handle(new SuspendUserCommand(adminId, adminId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownTarget_ReturnsNotFound()
    {
        var result = await _handler.Handle(new SuspendUserCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithActiveTarget_Succeeds()
    {
        var targetId = await SeedActiveUserAsync();

        var result = await _handler.Handle(new SuspendUserCommand(targetId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenAlreadySuspended_ReturnsConflict()
    {
        var targetId = await SeedActiveUserAsync();
        var actorId = Guid.NewGuid();
        await _handler.Handle(new SuspendUserCommand(targetId, actorId), CancellationToken.None);

        var result = await _handler.Handle(new SuspendUserCommand(targetId, actorId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AnotherAdminMayBeTargeted()
    {
        var otherAdminId = await SeedActiveUserAsync();

        var result = await _handler.Handle(new SuspendUserCommand(otherAdminId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
