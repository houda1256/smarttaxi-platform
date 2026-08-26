using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Notifications.Abstractions;

/// <summary>
/// Mandatory (security/legal/critical) notifications always resolve to the
/// platform's required channel set regardless of UserPreferences — they are
/// never disableable. Optional notifications resolve to the intersection of
/// the required minimum and the recipient's own UserPreferences.NotificationChannels
/// (Identity's existing table — Notifications introduces no competing
/// preferences store), falling back to UserPreferences' own defaults
/// (Email | InApp) when the recipient has no preferences row yet.
/// </summary>
public interface INotificationPreferencePolicy
{
    Task<IReadOnlyCollection<NotificationChannel>> ResolveChannelsAsync(
        Guid recipientUserId, bool isMandatory, CancellationToken cancellationToken);
}
