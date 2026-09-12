namespace SmartTaxi.API.Contracts.Identity;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
