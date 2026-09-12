namespace SmartTaxi.API.Contracts.Identity;

public sealed record TwoFactorChallengeResponse(string AccessToken, string RefreshToken);
