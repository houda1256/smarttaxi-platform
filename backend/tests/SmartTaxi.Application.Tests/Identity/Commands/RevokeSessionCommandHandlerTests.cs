using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RevokeSession;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RevokeSessionCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly RevokeSessionCommandHandler _revokeHandler;

    public RevokeSessionCommandHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());

        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy());
        _revokeHandler = new RevokeSessionCommandHandler(_sessionRepository);
    }

    private async Task<(Guid UserId, Guid SessionId)> RegisterAndLoginAsync(string email)
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand(email, "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand(email, "correct-password"), CancellationToken.None);

        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(registerResult.Value!.UserId, CancellationToken.None);
        return (registerResult.Value.UserId, sessions.Single().Id);
    }

    [Fact]
    public async Task Handle_RevokesOnlyTheTargetedSession_LeavingOtherSessionsForTheSameUserActive()
    {
        var (userId, firstSessionId) = await RegisterAndLoginAsync("user@example.com");
        // A second login for the same user creates a second, independent session.
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        var result = await _revokeHandler.Handle(new RevokeSessionCommand(userId, firstSessionId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var remainingActive = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Single(remainingActive);
        Assert.NotEqual(firstSessionId, remainingActive.Single().Id);
    }

    [Fact]
    public async Task Handle_ForSessionBelongingToAnotherUser_ReturnsNotFound()
    {
        var (_, sessionId) = await RegisterAndLoginAsync("owner@example.com");
        var otherUserId = Guid.NewGuid();

        var result = await _revokeHandler.Handle(new RevokeSessionCommand(otherUserId, sessionId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownSessionId_ReturnsNotFound()
    {
        var result = await _revokeHandler.Handle(new RevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
