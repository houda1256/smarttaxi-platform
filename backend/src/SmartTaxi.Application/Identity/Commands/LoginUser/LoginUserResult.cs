namespace SmartTaxi.Application.Identity.Commands.LoginUser;

public sealed record LoginUserResult(
    bool RequiresTwoFactor,
    string? AccessToken,
    string? RefreshToken,
    string? TwoFactorChallengeToken)
{
    public static LoginUserResult Authenticated(string accessToken, string refreshToken) =>
        new(false, accessToken, refreshToken, null);

    public static LoginUserResult TwoFactorRequired(string challengeToken) =>
        new(true, null, null, challengeToken);
}
