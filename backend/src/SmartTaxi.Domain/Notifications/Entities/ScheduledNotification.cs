using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Notifications.Entities;

/// <summary>
/// A reminder to dispatch later (e.g. "subscription expiring soon", a future
/// module's "document expiry"). Deliberately just a row a caller must sweep —
/// no Hangfire/Quartz/background worker is introduced; see
/// ProcessDueNotificationsCommand for the explicit, idempotent processing hook
/// a future scheduler (or an ops endpoint) calls.
/// </summary>
public sealed class ScheduledNotification : AggregateRoot
{
    public Guid RecipientUserId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public string TemplateKey { get; private set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Variables { get; private set; } = new Dictionary<string, string>();
    public bool IsMandatory { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; private set; }
    public ScheduledNotificationStatus Status { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ScheduledNotification()
    {
    }

    private ScheduledNotification(
        Guid recipientUserId, NotificationCategory category, string templateKey, IReadOnlyDictionary<string, string> variables,
        bool isMandatory, string sourceType, Guid sourceId, DateTime scheduledAtUtc, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RecipientUserId = recipientUserId;
        Category = category;
        TemplateKey = templateKey;
        Variables = variables;
        IsMandatory = isMandatory;
        SourceType = sourceType;
        SourceId = sourceId;
        IdempotencyKey = $"{sourceType}:{sourceId:N}:{recipientUserId:N}:{category}:{templateKey}";
        ScheduledAtUtc = scheduledAtUtc;
        Status = ScheduledNotificationStatus.Pending;
        CreatedAtUtc = utcNow;
    }

    public static ScheduledNotification Schedule(
        Guid recipientUserId, NotificationCategory category, string templateKey, IReadOnlyDictionary<string, string> variables,
        bool isMandatory, string sourceType, Guid sourceId, DateTime scheduledAtUtc, DateTime utcNow)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Le destinataire est requis.");
        }

        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw new ArgumentException("La clé du modèle est requise.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("Le type de source est requis.");
        }

        if (scheduledAtUtc < utcNow)
        {
            throw new ArgumentException("La date planifiée ne peut pas être dans le passé.");
        }

        return new ScheduledNotification(
            recipientUserId, category, templateKey.Trim(), variables, isMandatory, sourceType.Trim(), sourceId, scheduledAtUtc, utcNow);
    }

    public void MarkProcessed(DateTime utcNow)
    {
        Status = ScheduledNotificationStatus.Processed;
        ProcessedAtUtc = utcNow;
    }

    public void MarkFailed(DateTime utcNow)
    {
        Status = ScheduledNotificationStatus.Failed;
        ProcessedAtUtc = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        Status = ScheduledNotificationStatus.Cancelled;
        ProcessedAtUtc = utcNow;
    }
}
