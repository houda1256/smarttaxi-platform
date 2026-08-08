using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Sessions;

/// <summary>
/// Shared "generate raw token -> hash -> issue JWT" orchestration used by both
/// login (new session) and refresh (rotation within an existing session), so
/// the two handlers don't duplicate this logic.
/// </summary>
public sealed class RefreshTokenIssuer
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IRefreshTokenPolicy _policy;

    public RefreshTokenIssuer(
        ITokenGenerator tokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenPolicy policy)
    {
        _tokenGenerator = tokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _policy = policy;
    }

    public async Task<(UserSession Session, string RawRefreshToken, string AccessToken)> StartSessionAsync(
        User user, string? deviceLabel, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var rawToken = _refreshTokenGenerator.Generate();
        var tokenHash = _refreshTokenHasher.Hash(rawToken);

        var session = UserSession.Start(
            user.Id,
            tokenHash,
            utcNow.Add(_policy.TokenLifetime),
            utcNow.Add(_policy.SessionLifetime),
            deviceLabel,
            utcNow);

        var accessToken = await _tokenGenerator.GenerateToken(user, session.Id, cancellationToken);

        return (session, rawToken, accessToken);
    }

    public async Task<(TokenRotationResult RotationResult, string? RawRefreshToken, string? AccessToken)> RotateAsync(
        User user, UserSession session, string presentedTokenHash, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var newRawToken = _refreshTokenGenerator.Generate();
        var newTokenHash = _refreshTokenHasher.Hash(newRawToken);

        var rotationResult = session.RotateToken(presentedTokenHash, newTokenHash, utcNow.Add(_policy.TokenLifetime), utcNow);

        if (rotationResult.Outcome != TokenRotationOutcome.Success)
        {
            return (rotationResult, null, null);
        }

        var accessToken = await _tokenGenerator.GenerateToken(user, session.Id, cancellationToken);

        return (rotationResult, newRawToken, accessToken);
    }
}
