namespace SmartTaxi.Application.Identity.Queries.GetUserSessions;

public sealed record SessionSummaryResult(
    Guid SessionId,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    string? DeviceLabel,
    bool IsActive);
