namespace SmartTaxi.API.Contracts.Identity;

public sealed record SessionSummaryResponse(
    Guid SessionId,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    string? DeviceLabel,
    bool IsActive);
