using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RevokeAllSessions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RevokeAllSessionsCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly RevokeAllSessionsCommandHandler _revokeAllHandler;

    public RevokeAllSessionsCommandHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());

        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy());
        _revokeAllHandler = new RevokeAllSessionsCommandHandler(_sessionRepository);
    }

    [Fact]
    public async Task Handle_RevokesEveryActiveSessionForTheUser()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        var before = await _sessionRepository.GetActiveSessionsForUserAsync(registerResult.Value!.UserId, CancellationToken.None);
        Assert.Equal(3, before.Count);

        var result = await _revokeAllHandler.Handle(new RevokeAllSessionsCommand(registerResult.Value.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var after = await _sessionRepository.GetActiveSessionsForUserAsync(registerResult.Value.UserId, CancellationToken.None);
        Assert.Empty(after);
    }
}
