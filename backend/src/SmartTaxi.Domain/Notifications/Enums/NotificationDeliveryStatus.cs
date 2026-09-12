namespace SmartTaxi.Domain.Notifications.Enums;

/// <summary>
/// Transport status of a single delivery attempt on a single channel. Distinct
/// from Notification.IsRead, which is an InApp/user-interaction state, not a
/// transport outcome. Dev/logging senders (LoggingEmailSender, LoggingSmsSender,
/// the dev push sender) can only ever confirm the provider call itself
/// succeeded — Sent — never true carrier delivery, so Delivered is reserved for
/// channels/providers that can actually confirm it (none today).
/// </summary>
public enum NotificationDeliveryStatus
{
    Pending,
    Sent,
    Delivered,
    Failed
}
