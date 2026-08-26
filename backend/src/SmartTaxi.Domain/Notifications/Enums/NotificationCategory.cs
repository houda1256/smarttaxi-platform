namespace SmartTaxi.Domain.Notifications.Enums;

/// <summary>
/// Small, extensible catalog — one entry per existing module that can trigger a
/// notification today, plus System for platform-wide/non-module messages.
/// Future modules (e.g. Support/Administration, Module 11) add their own value
/// here rather than Notifications inventing speculative categories up front.
/// </summary>
public enum NotificationCategory
{
    Security,
    Identity,
    Ride,
    Payment,
    Subscription,
    Fleet,
    Loyalty,
    Advertising,
    Maintenance,
    Roadside,
    System
}
