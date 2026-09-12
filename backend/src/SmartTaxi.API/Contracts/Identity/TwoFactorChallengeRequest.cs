namespace SmartTaxi.API.Contracts.Identity;

public sealed record TwoFactorChallengeRequest(string ChallengeToken, string Code);
