namespace SmartTaxi.API.Contracts.Identity;

public sealed record RecoveryCodesResponse(IReadOnlyCollection<string> RecoveryCodes);
