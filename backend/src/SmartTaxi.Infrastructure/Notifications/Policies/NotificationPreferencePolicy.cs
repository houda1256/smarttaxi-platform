using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Infrastructure.Notifications.Options;

namespace SmartTaxi.Infrastructure.Notifications.Policies;

/// <summary>Mandatory notifications ignore UserPreferences entirely (never disableable); optional notifications resolve exactly to the recipient's own NotificationChannels, defaulting to UserPreferences' own default (Email | InApp) when no preferences row exists yet (e.g. a brand-new account).</summary>
internal sealed class NotificationPreferencePolicy : INotificationPreferencePolicy
{
    private const NotificationChannel DefaultChannelsWhenNoPreferences = NotificationChannel.Email | NotificationChannel.InApp;

    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly NotificationChannel _mandatoryChannels;

    public NotificationPreferencePolicy(IUserPreferencesRepository preferencesRepository, IOptions<NotificationOptions> options)
    {
        _preferencesRepository = preferencesRepository;
        _mandatoryChannels = options.Value.MandatoryChannels;
    }

    public async Task<IReadOnlyCollection<NotificationChannel>> ResolveChannelsAsync(
        Guid recipientUserId, bool isMandatory, CancellationToken cancellationToken)
    {
        if (isMandatory)
        {
            return ExpandFlags(_mandatoryChannels);
        }

        var preferences = await _preferencesRepository.GetByUserIdAsync(recipientUserId, cancellationToken);
        var channels = preferences?.NotificationChannels ?? DefaultChannelsWhenNoPreferences;

        return ExpandFlags(channels);
    }

    private static IReadOnlyCollection<NotificationChannel> ExpandFlags(NotificationChannel flags) =>
        Enum.GetValues<NotificationChannel>().Where(value => value != NotificationChannel.None && flags.HasFlag(value)).ToList();
}
