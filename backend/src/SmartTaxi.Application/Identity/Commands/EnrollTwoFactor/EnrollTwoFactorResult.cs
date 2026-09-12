namespace SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;

public sealed record EnrollTwoFactorResult(string Secret, string AuthenticatorUri);
