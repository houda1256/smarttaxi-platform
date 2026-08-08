namespace SmartTaxi.API.Contracts.Identity;

public sealed record DisableTwoFactorRequest(string CurrentPassword, string Code);
