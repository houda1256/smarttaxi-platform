using SmartTaxi.Domain.Identity.Preferences.Entities;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Identity.Preferences;

public sealed record UserPreferencesSummary(
    Guid UserId,
    Language Language,
    NotificationChannel NotificationChannels,
    bool ShareProfileWithPartners,
    bool AllowMarketingCommunications,
    string Timezone,
    string? DisplayName,
    string? AvatarUrl)
{
    public static UserPreferencesSummary FromEntity(UserPreferences preferences) => new(
        preferences.UserId,
        preferences.Language,
        preferences.NotificationChannels,
        preferences.ShareProfileWithPartners,
        preferences.AllowMarketingCommunications,
        preferences.Timezone,
        preferences.DisplayName,
        preferences.AvatarUrl);

    public static UserPreferencesSummary Defaults(Guid userId)
    {
        var defaults = UserPreferences.CreateDefault(userId, DateTime.UtcNow);
        return FromEntity(defaults);
    }
}
