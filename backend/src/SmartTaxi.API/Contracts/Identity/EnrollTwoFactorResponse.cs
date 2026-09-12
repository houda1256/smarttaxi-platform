namespace SmartTaxi.API.Contracts.Identity;

public sealed record EnrollTwoFactorResponse(string Secret, string AuthenticatorUri);
