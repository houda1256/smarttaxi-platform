using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Domain.Identity.Preferences.Entities;

/// <summary>
/// A single owner's own settings — no multi-actor contention, so this is a
/// plain load-mutate-save entity, unlike the atomic-guard entities elsewhere
/// in Identity that model reviewer-vs-owner races.
/// </summary>
public sealed class UserPreferences
{
    public Guid UserId { get; private set; }
    public Language Language { get; private set; }
    public NotificationChannel NotificationChannels { get; private set; }
    public bool ShareProfileWithPartners { get; private set; }
    public bool AllowMarketingCommunications { get; private set; }
    public string Timezone { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserPreferences()
    {
    }

    private UserPreferences(Guid userId, DateTime utcNow)
    {
        UserId = userId;
        Language = Language.French;
        NotificationChannels = NotificationChannel.Email | NotificationChannel.InApp;
        ShareProfileWithPartners = false;
        AllowMarketingCommunications = false;
        Timezone = "UTC";
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static UserPreferences CreateDefault(Guid userId, DateTime utcNow) => new(userId, utcNow);

    public void Update(
        Language language,
        NotificationChannel notificationChannels,
        bool shareProfileWithPartners,
        bool allowMarketingCommunications,
        string timezone,
        string? displayName,
        string? avatarUrl,
        DateTime utcNow)
    {
        Language = language;
        NotificationChannels = notificationChannels;
        ShareProfileWithPartners = shareProfileWithPartners;
        AllowMarketingCommunications = allowMarketingCommunications;
        Timezone = timezone;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        UpdatedAt = utcNow;
    }
}
