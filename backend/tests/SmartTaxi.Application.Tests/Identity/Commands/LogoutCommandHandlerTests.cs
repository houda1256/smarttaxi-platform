using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.Logout;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class LogoutCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly LogoutCommandHandler _logoutHandler;

    public LogoutCommandHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());

        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy(), new FakeLoginLockoutPolicy(), new FakeAuditLogRepository(), new FakeAuditContextAccessor());
        _logoutHandler = new LogoutCommandHandler(_sessionRepository);
    }

    [Fact]
    public async Task Handle_RevokesTheCurrentSession()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(registerResult.Value!.UserId, CancellationToken.None);
        var sessionId = sessions.Single().Id;

        var result = await _logoutHandler.Handle(new LogoutCommand(sessionId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var remainingActive = await _sessionRepository.GetActiveSessionsForUserAsync(registerResult.Value.UserId, CancellationToken.None);
        Assert.Empty(remainingActive);
    }

    [Fact]
    public async Task Handle_WithUnknownSessionId_IsIdempotentSuccess()
    {
        var result = await _logoutHandler.Handle(new LogoutCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
