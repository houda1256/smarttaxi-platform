using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Notifications.Abstractions;

/// <summary>
/// Resolves requested language -> configured default -> deterministic fallback
/// (see NotificationTemplateRenderer for the exact chain) and performs simple
/// "{VariableName}" substitution against an allow-listed variable dictionary —
/// never an expression/scripting engine, so there is no template-injection
/// surface.
/// </summary>
public interface INotificationTemplateRenderer
{
    Task<RenderedNotificationContent> RenderAsync(
        string templateKey, NotificationChannel channel, Language requestedLanguage,
        IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken);
}

public sealed record RenderedNotificationContent(string? Subject, string Body, bool UsedFallback);
