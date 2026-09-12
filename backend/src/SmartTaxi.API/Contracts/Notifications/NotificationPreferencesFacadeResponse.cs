using SmartTaxi.Application.Identity.Preferences;

namespace SmartTaxi.API.Contracts.Notifications;

/// <summary>
/// A read-only convenience view over Identity's own UserPreferences — Module 6
/// introduces no competing preferences store. Writing preferences (including
/// NotificationChannels) stays on Identity's existing
/// PUT /api/users/me/preferences endpoint; duplicating a write path here would
/// create two sources of truth for the same row.
/// </summary>
public sealed record NotificationPreferencesFacadeResponse(string Language, IReadOnlyCollection<string> NotificationChannels)
{
    public static NotificationPreferencesFacadeResponse FromSummary(UserPreferencesSummary summary) => new(
        summary.Language.ToString(), ExpandFlags(summary.NotificationChannels));

    private static IReadOnlyCollection<string> ExpandFlags(Domain.Identity.Preferences.Enums.NotificationChannel channels) =>
        Enum.GetValues<Domain.Identity.Preferences.Enums.NotificationChannel>()
            .Where(value => value != Domain.Identity.Preferences.Enums.NotificationChannel.None && channels.HasFlag(value))
            .Select(value => value.ToString())
            .ToList();
}
