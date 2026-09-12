using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Infrastructure.Notifications.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>The fixed channel set mandatory/security-critical notifications always use — never reduced by UserPreferences.</summary>
    public NotificationChannel MandatoryChannels { get; init; } = NotificationChannel.Email | NotificationChannel.InApp;

    /// <summary>No infinite retry — see INotificationRetryPolicy.</summary>
    public int RetryMaxAttempts { get; init; } = 3;

    public int RetryDelayMinutes { get; init; } = 15;
}
