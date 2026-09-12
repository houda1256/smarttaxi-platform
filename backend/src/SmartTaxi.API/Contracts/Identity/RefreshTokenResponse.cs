namespace SmartTaxi.API.Contracts.Identity;

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken);
