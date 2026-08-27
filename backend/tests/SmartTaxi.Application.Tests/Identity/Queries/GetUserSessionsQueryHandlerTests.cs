using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Queries.GetUserSessions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Queries;

public class GetUserSessionsQueryHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeSessionRepository _sessionRepository = new();
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly GetUserSessionsQueryHandler _queryHandler;

    public GetUserSessionsQueryHandlerTests()
    {
        var refreshTokenIssuer = new RefreshTokenIssuer(
            new FakeTokenGenerator(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeRefreshTokenPolicy());

        _loginHandler = new LoginUserCommandHandler(
            _userRepository, _passwordHasher, _sessionRepository, refreshTokenIssuer,
            new FakeTwoFactorChallengeRepository(), new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), new FakeTwoFactorPolicy(), new FakeLoginLockoutPolicy(), new FakeAuditLogRepository());
        _queryHandler = new GetUserSessionsQueryHandler(_sessionRepository);
    }

    [Fact]
    public async Task Handle_ReturnsSafeSummaryFieldsOnly_NoHashOrRawTokenData()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        await _loginHandler.Handle(new LoginUserCommand("user@example.com", "correct-password", "Test Device"), CancellationToken.None);

        var result = await _queryHandler.Handle(new GetUserSessionsQuery(registerResult.Value!.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summary = Assert.Single(result.Value!);
        Assert.Equal("Test Device", summary.DeviceLabel);
        Assert.True(summary.IsActive);
        // SessionSummaryResult's declared properties are exactly this fixed set —
        // there is no TokenHash/RefreshToken property to accidentally serialize.
        var propertyNames = typeof(SessionSummaryResult).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Equal(
            new HashSet<string> { "SessionId", "CreatedAt", "LastActivityAt", "ExpiresAt", "DeviceLabel", "IsActive" },
            propertyNames);
    }
}
