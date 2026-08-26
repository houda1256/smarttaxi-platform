using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RefreshToken;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly FakeRefreshTokenHasher _refreshTokenHasher = new();
    private readonly RefreshTokenIssuer _refreshTokenIssuer;
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), _refreshTokenHasher, new FakeRefreshTokenPolicy());

        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, _refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy());
        _refreshHandler = new RefreshTokenCommandHandler(_sessionRepository, _userRepository, _refreshTokenHasher, _refreshTokenIssuer);
    }

    private async Task<string> RegisterAndLoginAsync(string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);

        var loginResult = await _loginHandler.Handle(new LoginUserCommand(email, password), CancellationToken.None);
        Assert.True(loginResult.IsSuccess);
        return loginResult.Value!.RefreshToken!;
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_RotatesItAndReturnsNewTokens()
    {
        var refreshToken = await RegisterAndLoginAsync();

        var result = await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(refreshToken, result.Value!.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
    }

    [Fact]
    public async Task Handle_PresentingTheSameOldTokenAgain_IsRejected()
    {
        var refreshToken = await RegisterAndLoginAsync();
        await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        var secondAttempt = await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        Assert.False(secondAttempt.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, secondAttempt.ErrorType);
    }

    [Fact]
    public async Task Handle_TokenReuse_RevokesTheWholeSessionSoTheRotatedTokenAlsoStopsWorking()
    {
        var refreshToken = await RegisterAndLoginAsync();
        var firstRotation = await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);
        var rotatedToken = firstRotation.Value!.RefreshToken;

        // Reuse the original (now-replaced) token — this must revoke the session.
        await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        var attemptWithRotatedToken = await _refreshHandler.Handle(new RefreshTokenCommand(rotatedToken), CancellationToken.None);

        Assert.False(attemptWithRotatedToken.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, attemptWithRotatedToken.ErrorType);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsUnauthorizedWithoutRevokingSession()
    {
        var userId = await RegisterUserAndGetIdAsync();
        const string rawToken = "already-issued-raw-token";
        var tokenHash = _refreshTokenHasher.Hash(rawToken);
        var utcNow = DateTime.UtcNow;
        var session = SmartTaxi.Domain.Identity.Entities.UserSession.Start(
            userId, tokenHash, utcNow.AddMinutes(-1), utcNow.AddDays(90), null, utcNow.AddMinutes(-30));
        await _sessionRepository.AddAsync(session, CancellationToken.None);

        var result = await _refreshHandler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.True(session.IsActive(utcNow));
    }

    [Fact]
    public async Task Handle_WithRevokedSession_ReturnsUnauthorized()
    {
        var refreshToken = await RegisterAndLoginAsync();
        var tokenHash = _refreshTokenHasher.Hash(refreshToken);
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, CancellationToken.None);
        session!.Revoke(SessionRevocationReason.ManualRevocation, DateTime.UtcNow);
        await _sessionRepository.UpdateAsync(session, CancellationToken.None);

        var result = await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    private async Task<Guid> RegisterUserAndGetIdAsync(string email = "id-lookup@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var result = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        Assert.True(result.IsSuccess);
        return result.Value!.UserId;
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsUnauthorized()
    {
        var result = await _refreshHandler.Handle(new RefreshTokenCommand("never-issued-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_ReturnsUnauthorized()
    {
        var refreshToken = await RegisterAndLoginAsync();
        var user = (await _userRepository.GetByEmailAsync(
            SmartTaxi.Domain.Identity.ValueObjects.Email.Create("user@example.com"), CancellationToken.None))!;
        user.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Handle_RenewedAccessToken_IsIssuedThroughTheSameTokenGeneratorAsLogin()
    {
        // FakeTokenGenerator always reflects the user's *current* roles/permissions
        // at call time (it's regenerated fresh on every call, exactly like the real
        // JwtTokenGenerator queries IRolePermissionRepository fresh every time) — so
        // proving refresh calls the same ITokenGenerator abstraction as login is the
        // meaningful, fake-level guarantee that "current roles/permissions" carry over.
        var refreshToken = await RegisterAndLoginAsync();

        var result = await _refreshHandler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("token-for-", result.Value!.AccessToken);
        Assert.Contains("-session-", result.Value.AccessToken);
    }
}
