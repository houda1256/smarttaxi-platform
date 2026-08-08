namespace SmartTaxi.Application.Identity.Commands.RegenerateRecoveryCodes;

public sealed record RegenerateRecoveryCodesResult(IReadOnlyCollection<string> RecoveryCodes);
