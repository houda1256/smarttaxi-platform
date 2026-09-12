namespace SmartTaxi.API.Contracts.Identity;

public sealed record LoginResponse(
    bool RequiresTwoFactor,
    string? AccessToken,
    string? RefreshToken,
    string? TwoFactorChallengeToken);
