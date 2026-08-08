using SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;
using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;
using TwoFactorChallengeCommandNs = SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class TwoFactorChallengeCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly FakeTwoFactorChallengeRepository _challengeRepository = new();
    private readonly FakeTwoFactorRecoveryCodeRepository _recoveryCodeRepository = new();
    private readonly FakeTotpService _totpService = new();
    private readonly FakeTwoFactorSecretProtector _protector = new();
    private readonly FakeTwoFactorPolicy _policy = new();
    private readonly FakeRefreshTokenHasher _refreshTokenHasher = new();

    private readonly LoginUserCommandHandler _loginHandler;
    private readonly EnrollTwoFactorCommandHandler _enrollHandler;
    private readonly ConfirmTwoFactorCommandHandler _confirmHandler;
    private readonly TwoFactorChallengeCommandNs.TwoFactorChallengeCommandHandler _challengeHandler;
    private readonly RecoveryCodeService _recoveryCodeService;

    public TwoFactorChallengeCommandHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), _refreshTokenHasher, new FakeRefreshTokenPolicy());

        _recoveryCodeService = new RecoveryCodeService(
            _recoveryCodeRepository, new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), _policy);

        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            _challengeRepository, new FakeRefreshTokenGenerator(), _refreshTokenHasher, _policy);

        _enrollHandler = new EnrollTwoFactorCommandHandler(_userRepository, _totpService, _protector, _policy);
        _confirmHandler = new ConfirmTwoFactorCommandHandler(_userRepository, _totpService, _protector, _recoveryCodeService);

        _challengeHandler = new TwoFactorChallengeCommandNs.TwoFactorChallengeCommandHandler(
            _challengeRepository, _userRepository, _sessionRepository, _refreshTokenHasher,
            _totpService, _protector, _recoveryCodeService, refreshTokenIssuer);
    }

    private async Task<(Guid UserId, string ChallengeToken, string Secret, IReadOnlyCollection<string> RecoveryCodes)> RegisterEnrollAndLoginAsync(
        string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var registerResult = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        var userId = registerResult.Value!.UserId;

        var enrollResult = await _enrollHandler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);
        var code = FakeTotpService.CodeFor(enrollResult.Value!.Secret);
        var confirmResult = await _confirmHandler.Handle(new ConfirmTwoFactorCommand(userId, code), CancellationToken.None);

        var loginResult = await _loginHandler.Handle(new LoginUserCommand(email, password), CancellationToken.None);
        Assert.True(loginResult.Value!.RequiresTwoFactor);

        return (userId, loginResult.Value.TwoFactorChallengeToken!, enrollResult.Value.Secret, confirmResult.Value!.RecoveryCodes);
    }

    [Fact]
    public async Task Handle_WithCorrectTotpCode_IssuesSessionAndConsumesChallenge()
    {
        var (userId, challengeToken, secret, _) = await RegisterEnrollAndLoginAsync();
        var code = FakeTotpService.CodeFor(secret);

        var result = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(challengeToken, code), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));

        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Single(sessions);
    }

    [Fact]
    public async Task Handle_WithWrongCode_ReturnsUnauthorizedAndDoesNotCreateSession()
    {
        var (userId, challengeToken, _, _) = await RegisterEnrollAndLoginAsync();

        var result = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(challengeToken, "wrong-code"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var sessions = await _sessionRepository.GetActiveSessionsForUserAsync(userId, CancellationToken.None);
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task Handle_WithRecoveryCode_SucceedsAndConsumesItSingleUse()
    {
        var (_, challengeToken, _, recoveryCodes) = await RegisterEnrollAndLoginAsync();
        var rawRecoveryCode = recoveryCodes.First();

        var result = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(challengeToken, rawRecoveryCode), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var reused = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(challengeToken, rawRecoveryCode), CancellationToken.None);
        Assert.False(reused.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithAlreadyConsumedChallenge_CannotBeReplayed()
    {
        var (_, challengeToken, secret, _) = await RegisterEnrollAndLoginAsync();
        var code = FakeTotpService.CodeFor(secret);

        var first = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(challengeToken, code), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand(challengeToken, code), CancellationToken.None);
        Assert.False(second.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithUnknownChallengeToken_ReturnsUnauthorized()
    {
        var result = await _challengeHandler.Handle(
            new TwoFactorChallengeCommandNs.TwoFactorChallengeCommand("not-a-real-token", "000000"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
