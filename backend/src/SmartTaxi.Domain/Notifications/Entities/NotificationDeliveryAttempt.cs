using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Notifications.Entities;

/// <summary>
/// Immutable, append-only technical delivery log for one Notification on one
/// channel — same "history row, own table, bare Guid reference" convention as
/// PaymentTransactionHistory/RideStatusHistory. Channel is always a single flag
/// value here (Email OR Sms OR Push OR InApp), never a combination, even though
/// the underlying NotificationChannel enum is [Flags] for storing a user's
/// multi-channel preference set.
/// </summary>
public sealed class NotificationDeliveryAttempt : Entity
{
    public Guid NotificationId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }
    public int AttemptNumber { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }

    private NotificationDeliveryAttempt()
    {
    }

    private NotificationDeliveryAttempt(Guid notificationId, NotificationChannel channel, int attemptNumber, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        NotificationId = notificationId;
        Channel = channel;
        Status = NotificationDeliveryStatus.Pending;
        AttemptNumber = attemptNumber;
        AttemptedAtUtc = utcNow;
    }

    public static NotificationDeliveryAttempt Create(Guid notificationId, NotificationChannel channel, int attemptNumber, DateTime utcNow)
    {
        if (attemptNumber < 1)
        {
            throw new ArgumentException("Le numéro de tentative doit être positif.");
        }

        return new NotificationDeliveryAttempt(notificationId, channel, attemptNumber, utcNow);
    }

    /// <summary>For dev/logging providers, "Sent" means the provider call itself completed without throwing — never a real transport-delivery guarantee.</summary>
    public void MarkSent(DateTime utcNow, string? providerMessageId)
    {
        Status = NotificationDeliveryStatus.Sent;
        SentAtUtc = utcNow;
        ProviderMessageId = providerMessageId;
    }

    public void MarkDelivered(DateTime utcNow)
    {
        Status = NotificationDeliveryStatus.Delivered;
        DeliveredAtUtc = utcNow;
    }

    public void MarkFailed(DateTime utcNow, string failureCode, string failureMessage, DateTime? nextAttemptAtUtc)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("Un code d'échec est requis.");
        }

        Status = NotificationDeliveryStatus.Failed;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }
}
