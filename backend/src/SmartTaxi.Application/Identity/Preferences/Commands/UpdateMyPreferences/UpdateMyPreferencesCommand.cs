using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Preferences;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Identity.Preferences.Commands.UpdateMyPreferences;

public sealed record UpdateMyPreferencesCommand(
    Guid UserId,
    Language Language,
    NotificationChannel NotificationChannels,
    bool ShareProfileWithPartners,
    bool AllowMarketingCommunications,
    string Timezone,
    string? DisplayName,
    string? AvatarUrl) : ICommand<Result<UserPreferencesSummary>>;
