using SmartTaxi.Application.Identity.Preferences;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.API.Contracts.Identity.Preferences;

public sealed record PreferencesResponse(
    string Language,
    IReadOnlyCollection<string> NotificationChannels,
    bool ShareProfileWithPartners,
    bool AllowMarketingCommunications,
    string Timezone,
    string? DisplayName,
    string? AvatarUrl)
{
    public static PreferencesResponse FromSummary(UserPreferencesSummary summary) => new(
        summary.Language.ToString(),
        ExpandFlags(summary.NotificationChannels),
        summary.ShareProfileWithPartners,
        summary.AllowMarketingCommunications,
        summary.Timezone,
        summary.DisplayName,
        summary.AvatarUrl);

    private static IReadOnlyCollection<string> ExpandFlags(NotificationChannel channels) =>
        Enum.GetValues<NotificationChannel>()
            .Where(value => value != NotificationChannel.None && channels.HasFlag(value))
            .Select(value => value.ToString())
            .ToList();
}
