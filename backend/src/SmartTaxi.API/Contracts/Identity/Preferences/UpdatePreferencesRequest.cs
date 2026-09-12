namespace SmartTaxi.API.Contracts.Identity.Preferences;

public sealed record UpdatePreferencesRequest(
    string Language,
    IReadOnlyCollection<string> NotificationChannels,
    bool ShareProfileWithPartners,
    bool AllowMarketingCommunications,
    string Timezone,
    string? DisplayName,
    string? AvatarUrl);
