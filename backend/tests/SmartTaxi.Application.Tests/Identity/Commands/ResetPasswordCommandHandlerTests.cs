using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.ForgotPassword;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.ResetPassword;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakePasswordResetTokenRepository _tokenRepository = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly FakeRefreshTokenGenerator _tokenGenerator = new();
    private readonly FakeRefreshTokenHasher _tokenHasher = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly ForgotPasswordCommandHandler _forgotHandler;
    private readonly ResetPasswordCommandHandler _resetHandler;
    private readonly LoginUserCommandHandler _loginHandler;

    public ResetPasswordCommandHandlerTests()
    {
        _forgotHandler = new ForgotPasswordCommandHandler(
            _userRepository, _tokenRepository, _tokenGenerator, _tokenHasher, new FakePasswordResetPolicy(), _emailSender);
        _resetHandler = new ResetPasswordCommandHandler(
            _tokenRepository, _userRepository, _sessionRepository, _tokenHasher, _passwordHasher);

        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());
        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy(), new FakeLoginLockoutPolicy(), new FakeAuditLogRepository());
    }

    private async Task<Guid> RegisterAndRequestResetAsync(string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        await _forgotHandler.Handle(new ForgotPasswordCommand(email), CancellationToken.None);
        return registerResult.Value!.UserId;
    }

    [Fact]
    public async Task Handle_WithValidToken_ChangesPasswordAndSucceedsOnce()
    {
        var userId = await RegisterAndRequestResetAsync();
        var rawToken = _tokenGenerator.LastGenerated!;

        var result = await _resetHandler.Handle(new ResetPasswordCommand(rawToken, "brand-new-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(_passwordHasher.Verify(user!.PasswordHash.Value, "brand-new-password"));
    }

    [Fact]
    public async Task Handle_ReusingAnAlreadyConsumedToken_IsRejected()
    {
        await RegisterAndRequestResetAsync();
        var rawToken = _tokenGenerator.LastGenerated!;
        await _resetHandler.Handle(new ResetPasswordCommand(rawToken, "brand-new-password"), CancellationToken.None);

        var secondAttempt = await _resetHandler.Handle(new ResetPasswordCommand(rawToken, "another-password"), CancellationToken.None);

        Assert.False(secondAttempt.IsSuccess);
        Assert.Equal(ErrorType.Validation, secondAttempt.ErrorType);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_IsRejected()
    {
        var userId = await RegisterAndRequestResetAsync();
        var rawToken = _tokenGenerator.LastGenerated!;
        await _tokenRepository.InvalidateActiveForUserAsync(userId, DateTime.UtcNow, CancellationToken.None);

        var result = await _resetHandler.Handle(new ResetPasswordCommand(rawToken, "brand-new-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithTooShortNewPassword_ReturnsValidationFailure()
    {
        await RegisterAndRequestResetAsync();
        var rawToken = _tokenGenerator.LastGenerated!;

        var result = await _resetHandler.Handle(new ResetPasswordCommand(rawToken, "short"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_OnSuccess_RevokesEveryActiveSessionForTheUser()
    {
        var userId = await RegisterAndRequestResetAsync();
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        var sessionsBefore = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Equal(2, sessionsBefore.Count);

        var rawToken = _tokenGenerator.LastGenerated!;
        await _resetHandler.Handle(new ResetPasswordCommand(rawToken, "brand-new-password"), CancellationToken.None);

        var sessionsAfter = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Empty(sessionsAfter);
    }
}
