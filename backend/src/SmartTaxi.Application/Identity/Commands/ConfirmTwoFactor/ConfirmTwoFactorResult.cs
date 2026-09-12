namespace SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;

public sealed record ConfirmTwoFactorResult(IReadOnlyCollection<string> RecoveryCodes);
