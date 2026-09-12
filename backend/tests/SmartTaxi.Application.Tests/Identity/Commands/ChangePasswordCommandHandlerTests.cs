using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.ChangePassword;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class ChangePasswordCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly LoginUserCommandHandler _loginHandler;

    public ChangePasswordCommandHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());
        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy(), new FakeLoginLockoutPolicy(), new FakeAuditLogRepository(), new FakeAuditContextAccessor());
    }

    private ChangePasswordCommandHandler CreateHandler(bool revokeOthers = true) =>
        new(_userRepository, _passwordHasher, _sessionRepository, new FakeSecurityPolicy { RevokeOtherSessionsOnPasswordChange = revokeOthers });

    private async Task<Guid> RegisterUserAsync(string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var result = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        return result.Value!.UserId;
    }

    [Fact]
    public async Task Handle_WithCorrectCurrentPassword_ChangesPassword()
    {
        var userId = await RegisterUserAsync();
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangePasswordCommand(userId, Guid.NewGuid(), "correct-password", "brand-new-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(_passwordHasher.Verify(user!.PasswordHash.Value, "brand-new-password"));
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ReturnsUnauthorized()
    {
        var userId = await RegisterUserAsync();
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangePasswordCommand(userId, Guid.NewGuid(), "wrong-password", "brand-new-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithDefaultPolicy_KeepsCurrentSessionButRevokesOthers()
    {
        var userId = await RegisterUserAsync();
        var currentLogin = await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        var sessionsBefore = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Equal(2, sessionsBefore.Count);

        var currentSession = await _sessionRepository.GetByRefreshTokenHashAsync(
            new FakeRefreshTokenHasher().Hash(currentLogin.Value!.RefreshToken!), CancellationToken.None);

        var handler = CreateHandler(revokeOthers: true);
        await handler.Handle(
            new ChangePasswordCommand(userId, currentSession!.Id, "correct-password", "brand-new-password"), CancellationToken.None);

        var sessionsAfter = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Single(sessionsAfter);
        Assert.Equal(currentSession.Id, sessionsAfter.Single().Id);
    }

    [Fact]
    public async Task Handle_WithPolicyDisabled_KeepsAllSessionsActive()
    {
        var userId = await RegisterUserAsync();
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        var handler = CreateHandler(revokeOthers: false);
        await handler.Handle(
            new ChangePasswordCommand(userId, Guid.NewGuid(), "correct-password", "brand-new-password"), CancellationToken.None);

        var sessionsAfter = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Equal(2, sessionsAfter.Count);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_ReturnsUnauthorized()
    {
        var userId = await RegisterUserAsync();
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangePasswordCommand(userId, Guid.NewGuid(), "correct-password", "brand-new-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
