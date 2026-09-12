using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Tests.Identity.Entities;

public class UserSessionTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static UserSession CreateSession(string initialTokenHash = "initial-hash", DateTime? utcNow = null)
    {
        var now = utcNow ?? UtcNow;
        return UserSession.Start(
            userId: Guid.NewGuid(),
            initialTokenHash: initialTokenHash,
            tokenExpiresAt: now.AddMinutes(30),
            sessionExpiresAt: now.AddDays(90),
            deviceLabel: "Test Device",
            utcNow: now);
    }

    [Fact]
    public void Start_CreatesSessionWithOneInitialToken()
    {
        var session = CreateSession();

        Assert.Single(session.RefreshTokens);
        Assert.Equal("initial-hash", session.RefreshTokens.Single().TokenHash);
        Assert.True(session.IsActive(UtcNow));
    }

    [Fact]
    public void RotateToken_WithValidCurrentToken_ReplacesItAndReturnsSuccess()
    {
        var session = CreateSession();
        var rotateAt = UtcNow.AddMinutes(5);

        var result = session.RotateToken("initial-hash", "new-hash", rotateAt.AddMinutes(30), rotateAt);

        Assert.Equal(TokenRotationOutcome.Success, result.Outcome);
        Assert.NotNull(result.NewToken);
        Assert.Equal("new-hash", result.NewToken!.TokenHash);
        Assert.Equal(2, session.RefreshTokens.Count);
        Assert.True(session.RefreshTokens.First(t => t.TokenHash == "initial-hash").IsReplaced);
        Assert.Equal(rotateAt, session.LastActivityAt);
    }

    [Fact]
    public void RotateToken_PresentingAlreadyReplacedToken_DetectsReuseAndRevokesSession()
    {
        var session = CreateSession();
        session.RotateToken("initial-hash", "second-hash", UtcNow.AddMinutes(35), UtcNow.AddMinutes(5));

        var reuseResult = session.RotateToken("initial-hash", "attacker-hash", UtcNow.AddMinutes(40), UtcNow.AddMinutes(10));

        Assert.Equal(TokenRotationOutcome.ReuseDetected, reuseResult.Outcome);
        Assert.Null(reuseResult.NewToken);
        Assert.False(session.IsActive(UtcNow.AddMinutes(10)));
        Assert.Equal(SessionRevocationReason.ReuseDetected, session.RevokedReason);
        // The legitimate second token must also be unusable now that the session is revoked.
        var secondRotation = session.RotateToken("second-hash", "irrelevant-hash", UtcNow.AddMinutes(45), UtcNow.AddMinutes(11));
        Assert.Equal(TokenRotationOutcome.SessionRevoked, secondRotation.Outcome);
    }

    [Fact]
    public void RotateToken_AfterReuseDetected_StampsTheReusedTokenAsRevoked()
    {
        var session = CreateSession();
        session.RotateToken("initial-hash", "second-hash", UtcNow.AddMinutes(35), UtcNow.AddMinutes(5));

        session.RotateToken("initial-hash", "attacker-hash", UtcNow.AddMinutes(40), UtcNow.AddMinutes(10));

        var reusedToken = session.RefreshTokens.First(t => t.TokenHash == "initial-hash");
        Assert.True(reusedToken.IsRevoked);
    }

    [Fact]
    public void RotateToken_WithExpiredToken_ReturnsTokenExpiredWithoutRevokingSession()
    {
        var session = CreateSession();
        var afterTokenExpiry = UtcNow.AddMinutes(31);

        var result = session.RotateToken("initial-hash", "new-hash", afterTokenExpiry.AddMinutes(30), afterTokenExpiry);

        Assert.Equal(TokenRotationOutcome.TokenExpired, result.Outcome);
        Assert.True(session.IsActive(afterTokenExpiry));
    }

    [Fact]
    public void RotateToken_WhenSessionAlreadyRevoked_ReturnsSessionRevoked()
    {
        var session = CreateSession();
        session.Revoke(SessionRevocationReason.ManualRevocation, UtcNow);

        var result = session.RotateToken("initial-hash", "new-hash", UtcNow.AddMinutes(30), UtcNow.AddMinutes(1));

        Assert.Equal(TokenRotationOutcome.SessionRevoked, result.Outcome);
    }

    [Fact]
    public void RotateToken_WhenSessionPastAbsoluteExpiry_ReturnsSessionExpired()
    {
        var session = UserSession.Start(
            Guid.NewGuid(), "hash", UtcNow.AddDays(1), UtcNow.AddDays(1), null, UtcNow);

        var result = session.RotateToken("hash", "new-hash", UtcNow.AddDays(2), UtcNow.AddDays(2));

        Assert.Equal(TokenRotationOutcome.SessionExpired, result.Outcome);
    }

    [Fact]
    public void RotateToken_WithUnknownTokenHash_ReturnsTokenNotFound()
    {
        var session = CreateSession();

        var result = session.RotateToken("does-not-exist", "new-hash", UtcNow.AddMinutes(30), UtcNow.AddMinutes(1));

        Assert.Equal(TokenRotationOutcome.TokenNotFound, result.Outcome);
    }

    [Fact]
    public void Revoke_CalledTwice_IsIdempotent()
    {
        var session = CreateSession();

        session.Revoke(SessionRevocationReason.LoggedOut, UtcNow);
        var firstRevokedAt = session.RevokedAt;
        session.Revoke(SessionRevocationReason.ManualRevocation, UtcNow.AddMinutes(1));

        Assert.Equal(firstRevokedAt, session.RevokedAt);
        Assert.Equal(SessionRevocationReason.LoggedOut, session.RevokedReason);
    }
}
