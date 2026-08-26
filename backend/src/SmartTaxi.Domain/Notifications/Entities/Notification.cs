using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Notifications.Events;

namespace SmartTaxi.Domain.Notifications.Entities;

/// <summary>
/// One notification for one recipient about one business event. Title/Body are
/// rendered content snapshotted at creation time (same convention as
/// Invoice/Receipt snapshotting their source data) — editing a
/// NotificationTemplate later never rewrites history. IdempotencyKey is
/// deterministic from (SourceType, SourceId, RecipientUserId, Category) and
/// carries a DB unique index (see NotificationConfiguration), so a retried
/// business event never creates a duplicate notification for the same
/// recipient. Read state is a plain load-mutate-save transition — MarkAsRead is
/// idempotent (never errors on an already-read notification), so no atomic
/// repository-level guard is needed the way contested status transitions
/// (Subscription, Payment) require one.
/// </summary>
public sealed class Notification : AggregateRoot
{
    public Guid RecipientUserId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public string TemplateKey { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// The raw render variables, kept (not just the rendered Title/Body) so a
    /// later retry or a different channel's template can still be rendered
    /// with full fidelity — never exposed through any read API/DTO.
    /// </summary>
    public IReadOnlyDictionary<string, string> Variables { get; private set; } = new Dictionary<string, string>();

    public bool IsMandatory { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public bool IsRead => ReadAtUtc.HasValue;

    private Notification()
    {
    }

    private Notification(
        Guid recipientUserId, NotificationCategory category, string templateKey, string title, string body,
        IReadOnlyDictionary<string, string> variables, bool isMandatory, string sourceType, Guid sourceId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RecipientUserId = recipientUserId;
        Category = category;
        TemplateKey = templateKey;
        Title = title;
        Body = body;
        Variables = variables;
        IsMandatory = isMandatory;
        SourceType = sourceType;
        SourceId = sourceId;
        IdempotencyKey = ComputeIdempotencyKey(sourceType, sourceId, recipientUserId, category);
        CreatedAtUtc = utcNow;

        RaiseDomainEvent(new NotificationCreated(Id, recipientUserId, category, utcNow));
    }

    public static Notification Create(
        Guid recipientUserId, NotificationCategory category, string templateKey, string title, string body,
        IReadOnlyDictionary<string, string> variables, bool isMandatory, string sourceType, Guid sourceId, DateTime utcNow)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Le destinataire est requis.");
        }

        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw new ArgumentException("La clé du modèle est requise.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Le titre de la notification est requis.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Le contenu de la notification est requis.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("Le type de source est requis.");
        }

        return new Notification(
            recipientUserId, category, templateKey.Trim(), title.Trim(), body.Trim(), variables, isMandatory, sourceType.Trim(), sourceId, utcNow);
    }

    /// <summary>Deterministic per (business event, recipient, category) — never per channel, since one Notification fans out to several NotificationDeliveryAttempt rows.</summary>
    public static string ComputeIdempotencyKey(string sourceType, Guid sourceId, Guid recipientUserId, NotificationCategory category) =>
        $"{sourceType}:{sourceId:N}:{recipientUserId:N}:{category}";

    public void MarkAsRead(DateTime utcNow)
    {
        if (ReadAtUtc is not null)
        {
            return;
        }

        ReadAtUtc = utcNow;
        RaiseDomainEvent(new NotificationRead(Id, RecipientUserId, utcNow));
    }
}
