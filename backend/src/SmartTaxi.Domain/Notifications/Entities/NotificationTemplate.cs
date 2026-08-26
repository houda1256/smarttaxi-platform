using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Notifications.Entities;

/// <summary>
/// Admin-managed content for one (TemplateKey, Channel, Language) combination —
/// that triple carries a DB unique index (see NotificationTemplateConfiguration).
/// Body/Subject use plain "{VariableName}" placeholders resolved by
/// NotificationTemplateRenderer against an allow-listed variable dictionary —
/// deliberately not a scripting/expression engine, so there is no injection
/// surface to defend.
/// </summary>
public sealed class NotificationTemplate : AggregateRoot
{
    public string TemplateKey { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public Language Language { get; private set; }
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private NotificationTemplate()
    {
    }

    private NotificationTemplate(
        string templateKey, NotificationCategory category, NotificationChannel channel, Language language,
        string? subject, string body, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        TemplateKey = templateKey;
        Category = category;
        Channel = channel;
        Language = language;
        Subject = subject;
        Body = body;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static NotificationTemplate Create(
        string templateKey, NotificationCategory category, NotificationChannel channel, Language language,
        string? subject, string body, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw new ArgumentException("La clé du modèle est requise.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Le contenu du modèle est requis.");
        }

        return new NotificationTemplate(templateKey.Trim(), category, channel, language, subject?.Trim(), body.Trim(), utcNow);
    }

    public void UpdateContent(string? subject, string body, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Le contenu du modèle est requis.");
        }

        Subject = subject?.Trim();
        Body = body.Trim();
        Version++;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }
}
