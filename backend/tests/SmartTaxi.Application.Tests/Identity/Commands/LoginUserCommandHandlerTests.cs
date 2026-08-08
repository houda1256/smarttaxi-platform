using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class LoginUserCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly FakeTwoFactorChallengeRepository _challengeRepository = new();
    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());

        _handler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            _challengeRepository, new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy());
    }

    private async Task RegisterUserAsync(string email, string password)
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var result = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAccessTokenAndRefreshToken()
    {
        await RegisterUserAsync("user@example.com", "correct-password");

        var result = await _handler.Handle(
            new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CreatesExactlyOneSessionWithOneRefreshToken()
    {
        var registerResult = await RegisterUserAndGetIdAsync("user@example.com", "correct-password");

        await _handler.Handle(new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(registerResult, CancellationToken.None);
        Assert.Single(sessions);
        Assert.Single(sessions.Single().RefreshTokens);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_ReturnsUnauthorizedWithSameGenericMessage()
    {
        var userId = await RegisterUserAndGetIdAsync("user@example.com", "correct-password");
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _handler.Handle(
            new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.Equal("Email ou mot de passe incorrect.", result.Error);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsUnauthorizedWithGenericMessage()
    {
        var result = await _handler.Handle(
            new LoginUserCommand("unknown@example.com", "any-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.Equal("Email ou mot de passe incorrect.", result.Error);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsUnauthorizedWithSameGenericMessageAsUnknownEmail()
    {
        await RegisterUserAsync("user@example.com", "correct-password");

        var result = await _handler.Handle(
            new LoginUserCommand("user@example.com", "wrong-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.Equal("Email ou mot de passe incorrect.", result.Error);
    }

    [Fact]
    public async Task Handle_ForUserWithTwoFactorEnabled_ReturnsChallengeTokenInsteadOfSession()
    {
        var userId = await RegisterUserAndGetIdAsync("user@example.com", "correct-password");
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        EnableTwoFactorForTest(user!);
        await _userRepository.UpdateAsync(user!, CancellationToken.None);

        var result = await _handler.Handle(
            new LoginUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RequiresTwoFactor);
        Assert.NotNull(result.Value.TwoFactorChallengeToken);
        Assert.Null(result.Value.AccessToken);
        Assert.Null(result.Value.RefreshToken);
        Assert.Equal(1, _challengeRepository.Count);

        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Empty(sessions);
    }

    private static void EnableTwoFactorForTest(SmartTaxi.Domain.Identity.Entities.User user)
    {
        var utcNow = DateTime.UtcNow;
        user.BeginTwoFactorEnrollment("protected:secret", utcNow);
        user.ConfirmTwoFactorEnrollment(utcNow);
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ReturnsUnauthorized()
    {
        var result = await _handler.Handle(
            new LoginUserCommand("not-an-email", "any-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    private async Task<Guid> RegisterUserAndGetIdAsync(string email, string password)
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var result = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        Assert.True(result.IsSuccess);
        return result.Value!.UserId;
    }
}
