using SmartTaxi.Application.Administration.Commands.RevokeUserSessions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Administration.Commands;

public class RevokeUserSessionsCommandHandlerTests
{
    private readonly FakeAdminUserManagementRepository _repository = new();
    private readonly RevokeUserSessionsCommandHandler _handler;

    public RevokeUserSessionsCommandHandlerTests()
    {
        _handler = new RevokeUserSessionsCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithSelfTarget_ReturnsForbidden()
    {
        var adminId = Guid.NewGuid();
        _repository.SeedUser(adminId, activeSessionCount: 2);

        var result = await _handler.Handle(new RevokeUserSessionsCommand(adminId, adminId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownTarget_ReturnsNotFound()
    {
        var result = await _handler.Handle(new RevokeUserSessionsCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithActiveSessions_ReturnsRevokedCount()
    {
        var targetId = Guid.NewGuid();
        _repository.SeedUser(targetId, activeSessionCount: 3);

        var result = await _handler.Handle(new RevokeUserSessionsCommand(targetId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public async Task Handle_IsIdempotent_SecondCallReturnsZeroButStillSucceeds()
    {
        var targetId = Guid.NewGuid();
        _repository.SeedUser(targetId, activeSessionCount: 1);
        var actorId = Guid.NewGuid();
        await _handler.Handle(new RevokeUserSessionsCommand(targetId, actorId), CancellationToken.None);

        var result = await _handler.Handle(new RevokeUserSessionsCommand(targetId, actorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }
}
