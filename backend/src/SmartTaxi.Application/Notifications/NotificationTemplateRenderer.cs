using System.Text;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Notifications;

/// <summary>
/// Resolution chain: requested language -> ConfiguredDefaultLanguage (French,
/// matching UserPreferences.CreateDefault's own default) -> a deterministic,
/// always-available fallback so DispatchAsync never has nothing to send.
/// Substitution is plain "{VariableName}" -> value text replacement; any
/// placeholder without a matching key in the supplied dictionary is left
/// untouched rather than guessed at or thrown on.
/// </summary>
public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private const Language ConfiguredDefaultLanguage = Language.French;

    private readonly INotificationTemplateRepository _templateRepository;

    public NotificationTemplateRenderer(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<RenderedNotificationContent> RenderAsync(
        string templateKey, NotificationChannel channel, Language requestedLanguage,
        IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetAsync(templateKey, channel, requestedLanguage, cancellationToken);

        if (template is null && requestedLanguage != ConfiguredDefaultLanguage)
        {
            template = await _templateRepository.GetAsync(templateKey, channel, ConfiguredDefaultLanguage, cancellationToken);
        }

        if (template is null || !template.IsActive)
        {
            return BuildDeterministicFallback(templateKey, variables);
        }

        return new RenderedNotificationContent(
            Substitute(template.Subject, variables), Substitute(template.Body, variables)!, UsedFallback: false);
    }

    /// <summary>Always available (no template lookup, no locale dependency) — guarantees NotificationDispatcher can always render something rather than silently dropping a notification.</summary>
    private static RenderedNotificationContent BuildDeterministicFallback(
        string templateKey, IReadOnlyDictionary<string, string> variables)
    {
        var builder = new StringBuilder(templateKey);

        foreach (var pair in variables.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            builder.Append('\n').Append(pair.Key).Append(": ").Append(pair.Value);
        }

        return new RenderedNotificationContent(Subject: null, builder.ToString(), UsedFallback: true);
    }

    private static string? Substitute(string? text, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        foreach (var pair in variables)
        {
            text = text.Replace($"{{{pair.Key}}}", pair.Value, StringComparison.Ordinal);
        }

        return text;
    }
}
