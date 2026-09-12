namespace SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

public sealed record TwoFactorChallengeResult(string AccessToken, string RefreshToken);
