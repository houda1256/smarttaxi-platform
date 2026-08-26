namespace SmartTaxi.Domain.Notifications.Enums;

/// <summary>
/// Small, extensible catalog — one entry per existing module that can trigger a
/// notification today, plus System for platform-wide/non-module messages.
/// Support is Module 11's own value (Administration deliberately has none —
/// no Module 11 notification originates from a general "Administration"
/// concept in this pass). Future modules add their own value here rather
/// than Notifications inventing speculative categories up front.
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
    Support,
    System
}
